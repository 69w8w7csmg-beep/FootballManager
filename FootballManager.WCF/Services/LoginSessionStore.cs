// מטרה: שומר מיפוי מאסימון התחברות לחשבון בשרת.
// מי משתמש: FootballManagerService.
// פעולות: יצירת אסימון, חיפושו וביטולו.
// תלויות: ConcurrentDictionary ומחולל אקראי להצפנה.
using System.Collections.Concurrent;

namespace FootballManager.WCF.Services;

/// <summary>מאגר זמני בצד שרת של אסימוני Session. הוא יוצר אסימון אקראי, מחזיר את המשתמש שלו עד תפוגה ומוחק אותו ביציאה או כשאינו תקף.</summary>
public sealed class LoginSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

    /// <summary>יוצרת אסימון אקראי בן 32 בתים, מקשרת אותו ל־UserId וקובעת לו תפוגה של שמונה שעות. שירות Login מחזיר אותו ללקוח לאחר אימות מוצלח.</summary>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>מחרוזת שמייצגת את תוצאת הפעולה.</returns>
    public string Create(int userId)
    {
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        _sessions[token] = new SessionInfo(userId, DateTimeOffset.UtcNow.Add(SessionLifetime));
        return token;
    }

    /// <summary>מחפשת Session לפי אסימון, מסירה אותו אם פג תוקפו ומחזירה UserId פעיל או null. שירות WCF טוען לפי המזהה את החשבון והתפקיד ממסד הנתונים.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>מזהה אם נמצא, או null אם אין תוצאה.</returns>
    public int? FindUserId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(token, out var session))
        {
            return null;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return session.UserId;
    }

    /// <summary>מסירה אסימון Session כאשר המשתמש מתנתק או כשהשירות מגלה שהחשבון אינו תקף. אם האסימון ריק, לא מתבצע שינוי.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    public void Remove(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _sessions.TryRemove(token, out _);
        }
    }

    /// <summary>המחלקה SessionInfo מרכזת אחריות של שירות WCF בצד השרת. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
    private sealed record SessionInfo(int UserId, DateTimeOffset ExpiresAt);
}
