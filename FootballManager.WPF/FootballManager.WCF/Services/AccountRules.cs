namespace FootballManager.WCF.Services;

public static class AccountRules
{
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
