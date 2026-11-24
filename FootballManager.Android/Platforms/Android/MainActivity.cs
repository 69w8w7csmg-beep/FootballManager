// מטרה: מפעיל את חלון MAUI של Android בשלב 9.
// מי משתמש: מערכת Android.
// פעולות: מגדיר את פעילות הכניסה של היישום.
// תלויות: MauiAppCompatActivity וערכת FootballManagerTheme.
using Android.App;
using Android.Content.PM;

namespace FootballManager.AndroidClient;

[Activity(Theme = "@style/FootballManagerTheme", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class MainActivity : MauiAppCompatActivity;
