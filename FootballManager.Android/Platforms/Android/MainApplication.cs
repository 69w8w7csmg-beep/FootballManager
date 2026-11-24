// מטרה: מחבר את תהליך Android לפרויקט MAUI בשלב 9.
// מי משתמש: מערכת Android.
// פעולות: CreateMauiApp מעביר את האתחול ל־MauiProgram.
// תלויות: MauiApplication ו־MauiProgram.
using Android.App;
using Android.Runtime;

namespace FootballManager.AndroidClient;

[Application]
public sealed class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    /// <summary>יוצר את יישום MAUI כשהמערכת מפעילה את התהליך.</summary>
    /// <returns>יישום MAUI עם המסך ושירות התקשורת.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
