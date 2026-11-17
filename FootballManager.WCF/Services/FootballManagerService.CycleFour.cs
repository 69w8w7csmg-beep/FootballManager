// תפקיד הקובץ: מימוש פעולות C4 לשליפת נתונים, CRUD והרשאות.
// שכבה: שירות WCF בצד השרת.
// משתמשים: WcfServiceClient שולח לכאן בקשות עם אסימון משתמש.
// קשר בפרויקט: השירות בודק Role ומעביר פעולות תקינות למאגרי Access של C4.
using System.Data.OleDb;
using System.Net;
using CoreWCF;
using CoreWCF.Channels;
using FootballManager.Contracts;

namespace FootballManager.WCF.Services;

/// <summary>מימוש חוזה SOAP/WCF בצד השרת. הוא מקבל DTOs מהלקוח, מאמת חשבונות ו־Session, בודק הרשאות ושולח פעולות למאגרי Access; שכבת WPF אינה פונה למסד.</summary>
public sealed partial class FootballManagerService
{
    /// <summary>מופעלת כאשר WcfServiceClient מבקש את רשימת ההעברות. השרת מזהה את המשתמש מתוך sessionToken, מתיר גישה רק למאמן, קורא למאגר Access ומחזיר את הרשומות. למשתמש לא מחובר נקבע 401, לסוג אחר נקבע 403, ושגיאת Access מוחזרת כהודעת כשל.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public TransfersResponseDto GetTransfers(string sessionToken)
    {
        // השרת בודק את המשתמש וה־Role מתוך האסימון לפני כל גישה לתוכן.
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return new TransfersResponseDto { Message = denial };

        try
        {
            return new TransfersResponseDto
            {
                Succeeded = true,
                Message = "רשימת ההעברות נטענה.",
                Transfers = _transfers.GetTransfers()
            };
        }
        catch (OleDbException)
        {
            return new TransfersResponseDto { Message = "לא ניתן לטעון את ההעברות ממסד Access." };
        }
    }

    /// <summary>מופעלת בעקבות שמירת העברה דרך חוזה WCF. תחילה נבדקת הרשאת Manager, אחר כך מזהי השחקן והקבוצות, תאריך ומחיר. מזהה 0 מוסיף רשומה ומזהה קיים מעדכן אותה; המאגר שומר ב־Access והתוצאה מוחזרת ללקוח. OleDbException הופכת להודעת כשל.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto SaveTransfer(string sessionToken, TransferDto transfer)
    {
        // הרשאה נבדקת בשרת, כך שהלקוח אינו יכול לבחור לעצמו תפקיד.
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        // בודקים קלט לפני שמנסים לכתוב למסד, כולל קשר בין הקבוצות.
        if (transfer is null || transfer.TransferId < 0 || transfer.PlayerId <= 0 ||
            transfer.FromTeamId <= 0 || transfer.ToTeamId <= 0 ||
            transfer.FromTeamId == transfer.ToTeamId)
            return Failure("יש לבחור שחקן ושתי קבוצות שונות שקיימות במסד.");
        if (transfer.TransferDate.Year < 100 || transfer.Price < 0m ||
            transfer.Price > 922337203685477.5807m ||
            decimal.Round(transfer.Price, 4) != transfer.Price)
            return Failure("יש להזין תאריך תקין ומחיר שאינו שלילי, עם עד ארבע ספרות אחרי הנקודה.");

        try
        {
            // אותו מאגר מבדיל בין הוספה לעדכון לפי TransferId.
            return _transfers.Save(transfer)
                ? Success(transfer.TransferId == 0 ? "ההעברה נוספה ונשמרה." : "פרטי ההעברה עודכנו ונשמרו.")
                : Failure("ההעברה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("ההעברה לא נשמרה. יש לוודא שהשחקן והקבוצות קיימים.");
        }
    }

    /// <summary>מופעלת כאשר לקוח Manager מבקש למחוק העברה. השרת בודק את האסימון ואת התפקיד, מאמת מזהה חיובי, ומעביר את המחיקה למאגר Access. התשובה מציינת הצלחה, רשומה חסרה או שגיאת מסד.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transferId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto DeleteTransfer(string sessionToken, int transferId)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        if (transferId <= 0) return Failure("יש לבחור העברה קיימת.");

        try
        {
            return _transfers.Delete(transferId)
                ? Success("ההעברה נמחקה.")
                : Failure("ההעברה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("לא ניתן למחוק את ההעברה ממסד Access.");
        }
    }

    /// <summary>מופעלת כאשר WcfServiceClient מבקש את סטטיסטיקות המשחק. השרת בודק שהמשתמש המחובר הוא Manager, קורא את הנתונים דרך מאגר Access ומחזיר DTO של הרשומות. כשל הרשאה או שגיאת מסד מוחזרים כהודעת שירות.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public PlayerStatisticsResponseDto GetPlayerStatistics(string sessionToken)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return new PlayerStatisticsResponseDto { Message = denial };

        try
        {
            return new PlayerStatisticsResponseDto
            {
                Succeeded = true,
                Message = "סטטיסטיקות המשחק נטענו.",
                Statistics = _statistics.GetStatistics()
            };
        }
        catch (OleDbException)
        {
            return new PlayerStatisticsResponseDto { Message = "לא ניתן לטעון את הסטטיסטיקות ממסד Access." };
        }
    }

    /// <summary>מופעלת לשמירת סטטיסטיקה חדשה או לעדכון קיימת. השרת בודק הרשאת Manager, מזהי שחקן ומשחק, שערים, בישולים, דקות ודירוג 0–100. לאחר מכן הוא שומר ב־Access ומחזיר תוצאה; שגיאת קשר או OleDbException מוחזרת כהודעת כשל.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statistic">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto SavePlayerStatistic(string sessionToken, PlayerStatisticDto statistic)
    {
        // ההרשאה נבדקת גם כאן, ללא קשר לבדיקות הכפתור במסך.
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        // שדות מספריים ויחסי FK נבדקים בשרת לפני שמירת ה־DTO.
        if (statistic is null || statistic.StatisticId < 0 || statistic.PlayerId <= 0 || statistic.MatchId <= 0)
            return Failure("יש לבחור שחקן ומשחק קיימים.");
        if (statistic.Goals is < 0 or > short.MaxValue ||
            statistic.Assists is < 0 or > short.MaxValue ||
            statistic.MinutesPlayed is < 0 or > short.MaxValue ||
            !float.IsFinite(statistic.Rating) || statistic.Rating is < 0 or > 100)
            return Failure("יש להזין מספרים בין 0 ל־32767 ודירוג בין 0 ל־100.");

        try
        {
            return _statistics.Save(statistic)
                ? Success(statistic.StatisticId == 0 ? "הסטטיסטיקה נוספה ונשמרה." : "הסטטיסטיקה עודכנה ונשמרה.")
                : Failure("הסטטיסטיקה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("הסטטיסטיקה לא נשמרה. יש לוודא שהשחקן והמשחק קיימים.");
        }
    }

    /// <summary>מופעלת כאשר Manager מבקש למחוק רשומת סטטיסטיקה. השרת מאמת משתמש והרשאה, בודק מזהה, ומבקש מהמאגר למחוק. התשובה מפרטת הצלחה, היעדר רשומה או שגיאת Access.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statisticId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto DeletePlayerStatistic(string sessionToken, int statisticId)
    {
        var denial = CycleFourAccessError(sessionToken);
        if (denial is not null) return Failure(denial);
        if (statisticId <= 0) return Failure("יש לבחור סטטיסטיקה קיימת.");

        try
        {
            return _statistics.Delete(statisticId)
                ? Success("הסטטיסטיקה נמחקה.")
                : Failure("הסטטיסטיקה לא נמצאה.");
        }
        catch (OleDbException)
        {
            return Failure("לא ניתן למחוק את הסטטיסטיקה ממסד Access.");
        }
    }

    /// <summary>משותפת לכל שש פעולות C4. היא מאתרת משתמש לפי sessionToken; אורח מקבל 401, Manager ממשיך, ו־Player או Admin מקבלים 403. את ה־Role היא קוראת מרשומת המשתמש שבשרת ולא מנתון שהלקוח שולח.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
    private string? CycleFourAccessError(string token)
    {
        HttpStatusCode status;
        string explanation;
        if (!TryGetAuthenticatedUser(token, out var user))
        {
            status = HttpStatusCode.Unauthorized;
            explanation = "נדרשת התחברות פעילה.";
        }
        else if (user.Role == "Manager")
        {
            return null;
        }
        else
        {
            status = HttpStatusCode.Forbidden;
            explanation = "רק מנהל קבוצות רשאי לנהל העברות וסטטיסטיקות.";
        }

        OperationContext.Current.OutgoingMessageProperties[HttpResponseMessageProperty.Name] =
            new HttpResponseMessageProperty { StatusCode = status };
        return $"{(int)status} {status} · {explanation}";
    }
}
