// מטרה: הופך רשומת שחקן לבקשת ניתוח בשרת.
// מי משתמש: FootballManagerService.AnalyzePlayerAsync.
// פעולות: AnalyzePlayerAsync.
// תלויות: GeminiChatProvider ו־PlayerDto.
using FootballManager.Contracts;

namespace FootballManager.WCF.Services;

/// <summary>משתמש באותו חיבור Gemini שרתי גם לפעולת סקאוט השחקנים הקיימת.</summary>
public sealed class GeminiPlayerAnalysisProvider : PlayerAnalysisProvider
{
    private readonly GeminiChatProvider _chat;

    /// <summary>משתמש במתאם הבינה הקיים לניתוח שחקן.</summary>
    /// <param name="chat">מתאם הבינה שמשתמשים בו גם בצ׳אט.</param>
    public GeminiPlayerAnalysisProvider(GeminiChatProvider chat) => _chat = chat;

    /// <summary>שולח לספק את פרופיל השחקן שנשלף בשרת ומחזיר ניתוח בעברית.</summary>
    public override async Task<string> AnalyzePlayerAsync(PlayerDto player, CancellationToken cancellationToken = default)
    {
        try
        {
            var question = $"נתח בקצרה את השחקן {player.FirstName} {player.LastName}: " +
                $"גיל {player.Age}, עמדה {player.Position}, דירוג {player.Rating}, " +
                $"מהירות {player.Speed}, בעיטה {player.Shooting}, מסירה {player.Passing}, הגנה {player.Defense}. " +
                "ציין חוזקה אחת, נקודה לשיפור והתאמה לעמדה.";
            var answer = await _chat.AskAsync(question, [], [player], [], cancellationToken);
            return answer.Answer;
        }
        catch (GeminiChatException exception)
        {
            throw new PlayerAnalysisProviderException(exception.Message);
        }
    }
}
