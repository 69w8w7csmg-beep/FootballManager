// מטרה: בודק שם משתמש וסיסמה בשרת לפני יצירת חשבון.
// מי משתמש: FootballManagerService ו־Program.
// פעולות: ValidateUsername ו־ValidatePassword.
// תלויות: כללי אורך ותווים של החשבון.
namespace FootballManager.WCF.Services;

/// <summary>כללי שרת לבדיקת שם משתמש וסיסמה בהרשמה; השרת מפעיל אותם גם אם לקוח שולח בקשה ישירות.</summary>
public static class AccountRules
{
    /// <summary>בודקת שם משתמש: לא ריק, 3–30 תווים, אותיות לטיניות, ספרות או קו תחתון. מחזירה הודעת הסבר אם לא תקין, אחרת null. משמשת את שירות WCF לפני יצירת חשבון.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
    public static string? ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return "יש להזין שם משתמש.";
        }

        if (username.Length < 3 || username.Length > 30)
        {
            return "שם המשתמש צריך להכיל 3 עד 30 תווים.";
        }

        if (username.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
        {
            return "אפשר להשתמש רק באותיות באנגלית, ספרות וקו תחתון.";
        }

        return null;
    }

    /// <summary>בודקת שסיסמה קיימת ואורכה 8–128 תווים. מחזירה הודעת הסבר או null, והשירות עוצר הרשמה כשהבדיקה נכשלת.</summary>
    /// <param name="password">הסיסמה שהוקלדה, לפני ש־PasswordHasher יוצר ממנה גיבוב.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "יש להזין סיסמה.";
        }

        if (password.Length < 8 || password.Length > 128)
        {
            return "הסיסמה צריכה להכיל 8 עד 128 תווים.";
        }

        return null;
    }
}
