// מטרה: מנתח שחקן מהמסד עבור Player בשרת.
// מי משתמש: AiScoutViewModel דרך חוזה WCF.
// פעולות: AnalyzePlayerAsync ובדיקת הרשאת הסקאוט.
// תלויות: AccessFootballRepository ו־PlayerAnalysisProvider.
using System.Data.OleDb;
using System.Net;
using CoreWCF;
using CoreWCF.Channels;
using FootballManager.Contracts;

namespace FootballManager.WCF.Services;

/// <summary>פעולת סקאוט AI בשרת. היא מאמתת Player, שולפת פרופיל ציבורי ממסד Access ורק אז פונה לספק החיצוני.</summary>
public sealed partial class FootballManagerService
{
    private readonly PlayerAnalysisProvider _playerAnalysis;

    /// <summary>מבקשת ניתוח שחקן דרך חוזה WCF. השרת דוחה אורח בתשובת 401 ותפקידים אחרים ב־403 לפני שליפת הנתונים או שימוש בספק AI.</summary>
    /// <param name="sessionToken">אסימון התחברות שמאפשר לשרת לזהות משתמש ותפקיד מתוך מסד הנתונים.</param>
    /// <param name="playerId">מזהה השחקן שיש לנתח.</param>
    /// <returns>תשובת ניתוח או הודעת כשל ידידותית.</returns>
    public async Task<PlayerAnalysisResponseDto> AnalyzePlayerAsync(string sessionToken, int playerId)
    {
        var denial = AiScoutAccessError(sessionToken);
        if (denial is not null)
        {
            return new PlayerAnalysisResponseDto { Message = denial };
        }

        if (playerId <= 0)
        {
            return new PlayerAnalysisResponseDto { Message = "יש לבחור שחקן קיים לניתוח." };
        }

        try
        {
            var player = _football.FindPlayer(playerId);
            if (player is null)
            {
                return new PlayerAnalysisResponseDto { Message = "השחקן שנבחר לא נמצא." };
            }

            // הספק מקבל DTO ציבורי בלבד, ולא מקבל גישה ישירה למסד או לפרטי חשבון.
            var analysis = await _playerAnalysis.AnalyzePlayerAsync(player);
            return new PlayerAnalysisResponseDto
            {
                Succeeded = true,
                Message = "ניתוח השחקן הושלם.",
                PlayerName = $"{player.FirstName} {player.LastName}",
                Analysis = analysis
            };
        }
        catch (PlayerAnalysisProviderException exception)
        {
            return new PlayerAnalysisResponseDto { Message = exception.Message };
        }
        catch (OperationCanceledException)
        {
            return new PlayerAnalysisResponseDto { Message = "הניתוח ארך זמן רב מדי. בדוק את החיבור ונסה שוב." };
        }
        catch (HttpRequestException)
        {
            return new PlayerAnalysisResponseDto { Message = "לא ניתן להתחבר לשירות הניתוח כרגע. בדוק את החיבור ונסה שוב." };
        }
        catch (OleDbException)
        {
            return new PlayerAnalysisResponseDto { Message = "לא ניתן לטעון את פרטי השחקן ממסד Access." };
        }
    }

    /// <summary>בודקת אסימון מול משתמש קיים ומתירה את סקאוט ה־AI לתפקיד Player בלבד. היא קובעת 401 לאורח ו־403 לתפקיד אחר.</summary>
    /// <param name="token">אסימון התחברות שהשרת מנפיק בעת כניסה.</param>
    /// <returns>הודעת דחייה אם המשתמש אינו מורשה, או null אם הוא Player פעיל.</returns>
    private string? AiScoutAccessError(string token)
    {
        HttpStatusCode status;
        string explanation;
        if (!TryGetAuthenticatedUser(token, out var user))
        {
            status = HttpStatusCode.Unauthorized;
            explanation = "נדרשת התחברות כשחקן.";
        }
        else if (user.Role == "Player")
        {
            return null;
        }
        else
        {
            status = HttpStatusCode.Forbidden;
            explanation = "רק משתמש Player רשאי להשתמש בניתוח AI.";
        }

        SetAccessDenial(status);
        return $"{(int)status} {status} · {explanation}";
    }
}
