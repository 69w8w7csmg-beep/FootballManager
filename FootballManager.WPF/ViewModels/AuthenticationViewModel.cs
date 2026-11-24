// מטרה: בודק קלט בטופסי החשבון של WPF.
// מי משתמש: MainWindow.
// פעולות: בדיקת שם משתמש, סיסמה ואישור סיסמה.
// תלויות: ערכי טופס החשבון ומצב האינטראקציה.
// ולידציה של טופסי כניסה והרשמה. החלון מציג את התוצאה בלבד.
namespace FootballManager.WPF.ViewModels;

/// <summary>ViewModel שמבצע בדיקות קלט מקומיות לטופסי כניסה והרשמה ומחזיר הודעות וכשירות לשליחה; הוא אינו מאמת משתמש מול המסד.</summary>
public sealed class AuthenticationViewModel
{
    public bool LoginUsernameTouched { get; set; }
    public bool LoginPasswordTouched { get; set; }
    public bool RegisterUsernameTouched { get; set; }
    public bool RegisterPasswordTouched { get; set; }
    public bool RegisterConfirmationTouched { get; set; }

    /// <summary>בודקת שם משתמש וסיסמה באמצעות פונקציות האימות, מציגה הודעת שגיאה רק לשדה שהמשתמש התחיל למלא, ומחזירה אם הטופס תקין להפעלה. MainWindow משתמש בתוצאה כדי לעדכן את כפתור הכניסה.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="password">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <returns>ערך מסוג LoginValidation שמשמש את הקוד הקורא.</returns>
    public LoginValidation EvaluateLogin(string? username, string? password)
    {
        var usernameError = ValidateUsername(username);
        var passwordError = ValidatePassword(password);
        var message = LoginUsernameTouched && usernameError is not null
            ? usernameError
            : LoginPasswordTouched && passwordError is not null
                ? passwordError
                : string.Empty;

        return new LoginValidation(message, usernameError is null && passwordError is null);
    }

    /// <summary>בודקת שם משתמש, סיסמה והתאמה בין הסיסמה לאישור. מחזירה רמזים ליד שדות שהמשתמש שינה, הודעת מצב ודגל CanSubmit עבור כפתור ההרשמה.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="password">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <param name="confirmation">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>ערך מסוג RegistrationValidation שמשמש את הקוד הקורא.</returns>
    public RegistrationValidation EvaluateRegistration(
        string? username, string? password, string? confirmation)
    {
        var usernameError = ValidateUsername(username);
        var passwordError = ValidatePassword(password);
        var confirmationError = string.IsNullOrEmpty(confirmation)
            ? "יש להזין שוב את הסיסמה לאישור."
            : password != confirmation
                ? "הסיסמאות אינן תואמות."
                : null;
        var canSubmit = usernameError is null && passwordError is null && confirmationError is null;
        var hasStartedTyping = RegisterUsernameTouched || RegisterPasswordTouched || RegisterConfirmationTouched;
        var message = !hasStartedTyping
            ? string.Empty
            : canSubmit
                ? "הפרטים תקינים ואפשר להמשיך."
                : "ההערות יופיעו ליד השדות בזמן ההקלדה.";

        return new RegistrationValidation(
            RegisterUsernameTouched ? usernameError : null,
            RegisterPasswordTouched ? passwordError : null,
            RegisterConfirmationTouched ? confirmationError : null,
            message,
            canSubmit);
    }

    /// <summary>בודקת שהשם אינו ריק, באורך 3–30 תווים ומכיל אותיות אנגליות, ספרות או קו תחתון בלבד. מוחזרת הודעה אם אינו תקין ו־null אם עבר את הבדיקה; השרת מבצע בדיקה מקבילה לפני שמירה.</summary>
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

        return username.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_')
            ? "אפשר להשתמש רק באותיות באנגלית, ספרות וקו תחתון."
            : null;
    }

    /// <summary>בודקת שסיסמה הוזנה ושאורכה 8–128 תווים. מחזירה הודעת שגיאה או null. זו בדיקת ממשק מקדימה; השירות מבצע בדיקה נוספת.</summary>
    /// <param name="password">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "יש להזין סיסמה.";
        }

        return password.Length < 8 || password.Length > 128
            ? "הסיסמה צריכה להכיל 8 עד 128 תווים."
            : null;
    }
}

/// <summary>המחלקה LoginValidation מרכזת אחריות של ViewModel של WPF. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
public readonly record struct LoginValidation(string Message, bool CanSubmit);

/// <summary>המחלקה RegistrationValidation מרכזת אחריות של ViewModel של WPF. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
public readonly record struct RegistrationValidation(
    string? UsernameHint, string? PasswordHint, string? ConfirmationHint,
    string Message, bool CanSubmit);
