// מטרה: פותח את המסך הראשי של לקוח Android בשלב 9.
// מי משתמש: MauiProgram ומערכת MAUI.
// פעולות: CreateWindow יוצר חלון ל־MainPage.
// תלויות: המסך MainPage שמתקבל בהזרקת תלויות.
using FootballManager.AndroidClient.Views;

namespace FootballManager.AndroidClient;

public sealed class App(MainPage page) : Application
{
    /// <summary>עוטף את תצוגת Android בחלון MAUI.</summary>
    /// <param name="activationState">מידע ההפעלה שמספקת הפלטפורמה.</param>
    /// <returns>חלון FootballManager.</returns>
    protected override Window CreateWindow(IActivationState? activationState) => new(page);
}
