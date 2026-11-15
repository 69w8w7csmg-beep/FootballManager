// ולידציה של טופסי כניסה והרשמה. החלון מציג את התוצאה בלבד.
namespace FootballManager.WPF.ViewModels;

public sealed class AuthenticationViewModel
{
    public bool LoginUsernameTouched { get; set; }
    public bool LoginPasswordTouched { get; set; }
    public bool RegisterUsernameTouched { get; set; }
    public bool RegisterPasswordTouched { get; set; }
    public bool RegisterConfirmationTouched { get; set; }

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

public readonly record struct LoginValidation(string Message, bool CanSubmit);

public readonly record struct RegistrationValidation(
    string? UsernameHint, string? PasswordHint, string? ConfirmationHint,
    string Message, bool CanSubmit);
