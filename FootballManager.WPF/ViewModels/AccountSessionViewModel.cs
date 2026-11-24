// מטרה: שומר בזיכרון הלקוח את פרטי ההתחברות שאישר השרת.
// מי משתמש: MainWindow, MatchesViewModel, Cycle4ContentViewModel וחלונות הבינה.
// פעולות: Start ו־Clear של מצב Session.
// תלויות: LoginResponseDto מהחוזה.
using FootballManager.Contracts;

namespace FootballManager.WPF.ViewModels;

/// <summary>אובייקט Session בצד WPF שמחזיק את שם המשתמש, Role ואסימון ההפעלה שהתקבלו מהשרת, ומנקה אותם ביציאה.</summary>
public sealed class AccountSessionViewModel
{
    public string SessionToken { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    /// <summary>מכינה או מאתחלת את רכיב המערכת המתואר בשם Start לפני השימוש בו. נקראת בקוד מתוך Login_Click.</summary>
    /// <param name="login">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    public void Start(LoginResponseDto login)
    {
        SessionToken = login.SessionToken;
        Username = login.Username;
        Role = login.Role;
    }

    /// <summary>מסיימת או מנקה את המצב Clear כדי להחזיר את הרכיב למצב המתאים. נקראת בקוד מתוך Clear Player Form_Click, Clear Team Form_Click.</summary>
    public void Clear()
    {
        SessionToken = string.Empty;
        Username = string.Empty;
        Role = string.Empty;
    }
}
