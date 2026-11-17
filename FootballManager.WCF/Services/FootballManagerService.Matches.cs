// פעולות משחקים ב־WCF, כולל אימות הרשאה לכל אחד מארבעת סוגי המשתמשים.
using System.Data.OleDb;
using System.Net;
using CoreWCF;
using CoreWCF.Channels;
using FootballManager.Contracts;

namespace FootballManager.WCF.Services;

/// <summary>מימוש חוזה SOAP/WCF בצד השרת. הוא מקבל DTOs מהלקוח, מאמת חשבונות ו־Session, בודק הרשאות ושולח פעולות למאגרי Access; שכבת WPF אינה פונה למסד.</summary>
public sealed partial class FootballManagerService
{
    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Matches ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Get Matches, Get Matches Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="searchText">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="matchDate">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public MatchesResponseDto GetMatches(string sessionToken, string searchText, DateTime? matchDate)
    {
        var denial = MatchAccessError(sessionToken, write: false);
        if (denial != null) return new MatchesResponseDto { Message = denial };
        if (matchDate?.Year < 100 || searchText?.Length > 255)
            return new MatchesResponseDto { Message = "מסנן התאריך או שם הקבוצה אינו תקין." };

        return new MatchesResponseDto { Succeeded = true, Matches = _matches.GetMatches(searchText, matchDate) };
    }

    /// <summary>מבצעת את פעולת הנתונים Save Match. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Save Match Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="match">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto SaveMatch(string sessionToken, MatchDto match)
    {
        var denial = MatchAccessError(sessionToken, write: true);
        if (denial != null) return Failure(denial);
        if (match is null || match.MatchId < 0 || match.HomeTeamId <= 0 || match.AwayTeamId <= 0 ||
            match.HomeTeamId == match.AwayTeamId)
            return Failure("יש לבחור שתי קבוצות שונות שקיימות במערכת.");
        if (match.MatchDate.Year < 100 || match.HomeScore is < 0 or > short.MaxValue ||
            match.AwayScore is < 0 or > short.MaxValue)
            return Failure("יש להזין תאריך תקין ותוצאה בין 0 ל־32767 לכל קבוצה.");

        try
        {
            return _matches.Save(match)
                ? Success(match.MatchId == 0 ? "המשחק נוסף ונשמר." : "פרטי המשחק והתוצאה נשמרו.")
                : Failure("המשחק לא נמצא.");
        }
        catch (OleDbException)
        {
            return Failure("המשחק לא נשמר. יש לוודא ששתי הקבוצות קיימות ולנסות שוב.");
        }
    }

    /// <summary>מבצעת את פעולת הנתונים Delete Match. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Delete Match Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="matchId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto DeleteMatch(string sessionToken, int matchId)
    {
        var denial = MatchAccessError(sessionToken, write: true);
        if (denial != null) return Failure(denial);
        if (matchId <= 0) return Failure("יש לבחור משחק קיים.");
        try
        {
            return _matches.Delete(matchId) ? Success("המשחק נמחק.") : Failure("המשחק לא נמצא.");
        }
        catch (OleDbException)
        {
            return Failure("המשחק לא נמחק. לא ניתן למחוק משחק המקושר לסטטיסטיקה של שחקן.");
        }
    }

    /// <summary>מבצעת את שלב המערכת Match Access Error באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Get Matches, Save Match.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="write">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
    private string? MatchAccessError(string token, bool write)
    {
        HttpStatusCode status;
        string explanation;
        if (!TryGetAuthenticatedUser(token, out var user))
        {
            status = HttpStatusCode.Unauthorized;
            explanation = "נדרשת התחברות פעילה.";
        }
        else if (user.Role == "Manager" || (!write && user.Role == "Player"))
        {
            return null;
        }
        else
        {
            status = HttpStatusCode.Forbidden;
            explanation = "לחשבון זה אין הרשאה לפעולה.";
        }

        OperationContext.Current.OutgoingMessageProperties[HttpResponseMessageProperty.Name] =
            new HttpResponseMessageProperty { StatusCode = status };
        return $"{(int)status} {status} · {explanation}";
    }
}
