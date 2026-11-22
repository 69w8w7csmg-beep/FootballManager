using System.Data.OleDb;
using System.Net;
using FootballManager.Contracts;

namespace FootballManager.WCF.Services;

/// <summary>צ'אט AI דרך חוזה WCF; בודק זהות בשרת, שולף נתונים מ־Access ושומר היסטוריה לפי משתמש.</summary>
public sealed partial class FootballManagerService
{
    private readonly GeminiChatProvider _geminiChat;
    private readonly AiChatHistoryStore _chatHistory;

    /// <summary>מטפלת בשאלה או באיפוס שיחה. רק Player מחובר רשאי לבצע את הפעולה.</summary>
    public async Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request)
    {
        if (request is null || !TryGetAuthenticatedUser(request.SessionToken, out var user))
        {
            SetAccessDenial(HttpStatusCode.Unauthorized);
            return new AiChatResponseDto { Message = "401 Unauthorized · נדרשת התחברות כשחקן." };
        }
        if (user.Role != "Player")
        {
            SetAccessDenial(HttpStatusCode.Forbidden);
            return new AiChatResponseDto { Message = "403 Forbidden · הצ'אט זמין לשחקן בלבד." };
        }
        var question = request.Question?.Trim() ?? string.Empty;
        if ((!request.ResetHistory || question.Length > 0) && (question.Length < 3 || question.Length > 1000))
        {
            SetAccessDenial(HttpStatusCode.BadRequest);
            return new AiChatResponseDto { Message = "400 Bad Request · יש להזין שאלה באורך 3 עד 1000 תווים." };
        }

        var conversation = _chatHistory.ForUser(user.UserId);
        await conversation.Gate.WaitAsync();
        try
        {
            if (request.ResetHistory && question.Length == 0)
            {
                conversation.Turns.Clear();
                return new AiChatResponseDto { Succeeded = true, Message = "היסטוריית השיחה אופסה." };
            }
            try
            {
                var players = _football.GetPlayers(string.Empty, string.Empty, 0, 100);
                var favorites = _football.GetFavorites(user.UserId);
                var history = request.ResetHistory ? [] : conversation.Turns.ToList();
                var result = await _geminiChat.AskAsync(question, history, players, favorites);
                var addedId = 0;
                // פלט המודל הוא הצעה בלבד. השרת מאמת מזהה קיים, בקשה מפורשת ובעלות לפני שינוי Access.
                var mentionedPlayers = players.Where(p =>
                    question.Contains($"{p.FirstName} {p.LastName}", StringComparison.OrdinalIgnoreCase)).ToList();
                if (result.FavoritePlayerId > 0 &&
                    question.Contains("מועדפ", StringComparison.OrdinalIgnoreCase) &&
                    mentionedPlayers.Count == 1 && mentionedPlayers[0].PlayerId == result.FavoritePlayerId &&
                    favorites.All(p => p.PlayerId != result.FavoritePlayerId))
                {
                    _football.AddFavorite(user.UserId, result.FavoritePlayerId);
                    addedId = result.FavoritePlayerId;
                }
                if (request.ResetHistory) conversation.Turns.Clear();
                conversation.Turns.Add(new AiChatTurn(true, question));
                conversation.Turns.Add(new AiChatTurn(false, result.Answer));
                if (conversation.Turns.Count > 20) conversation.Turns.RemoveRange(0, conversation.Turns.Count - 20);
                return new AiChatResponseDto
                {
                    Succeeded = true, Message = addedId > 0 ? "השחקן נוסף למועדפים." : "התשובה התקבלה.",
                    Answer = result.Answer, AddedFavoritePlayerId = addedId
                };
            }
            catch (GeminiChatException exception)
            {
                SetAccessDenial(exception.Status);
                return new AiChatResponseDto { Message = $"{(int)exception.Status} {exception.Status} · {exception.Message}" };
            }
            catch (OleDbException)
            {
                SetAccessDenial(HttpStatusCode.ServiceUnavailable);
                return new AiChatResponseDto { Message = "503 Service Unavailable · מסד הנתונים אינו זמין כרגע." };
            }
        }
        finally { conversation.Gate.Release(); }
    }
}
