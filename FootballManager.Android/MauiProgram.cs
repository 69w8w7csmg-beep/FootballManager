// מטרה: מרכיב את לקוח MAUI ל־Android בשלב 9.
// מי משתמש: MainApplication.
// פעולות: CreateMauiApp רושם את המסך ואת מתאם WCF.
// תלויות: App, MainPage, WcfClient ו־HttpClient.
using Microsoft.Extensions.DependencyInjection;
using FootballManager.AndroidClient.Services;
using FootballManager.AndroidClient.Views;

namespace FootballManager.AndroidClient;

public static class MauiProgram
{
    /// <summary>מרכיב את יישום הטלפון ואת השירותים שהוא צריך.</summary>
    /// <returns>מופע MAUI ששכבת Android יכולה להפעיל.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(15) });
        builder.Services.AddSingleton<WcfClient>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
