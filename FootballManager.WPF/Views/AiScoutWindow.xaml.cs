using System.Windows;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

/// <summary>חלון WPF להצגת ניתוח AI של שחקן. הוא משתמש ב־ViewModel וב־WCF בלבד ואינו מתחבר למסד Access.</summary>
public partial class AiScoutWindow : Window
{
    private readonly AiScoutViewModel _viewModel;

    /// <summary>יוצרת חלון סקאוט עם מתאם WCF ו־Session של החשבון הפעיל.</summary>
    /// <param name="serviceClient">רכיב שמבצע קריאות לחוזה WCF.</param>
    /// <param name="session">אסימון ושם המשתמש שהתקבלו לאחר התחברות.</param>
    public AiScoutWindow(WcfServiceClient serviceClient, AccountSessionViewModel session)
    {
        InitializeComponent();
        _viewModel = new AiScoutViewModel(serviceClient, session);
        DataContext = _viewModel;
        Loaded += Window_Loaded;
    }

    /// <summary>טוענת את רשימת השחקנים דרך WCF כשהחלון נפתח.</summary>
    /// <param name="sender">החלון ששלח את אירוע הטעינה.</param>
    /// <param name="e">פרטי אירוע הטעינה של WPF.</param>
    private async void Window_Loaded(object sender, RoutedEventArgs e) => await _viewModel.LoadPlayersAsync();

    /// <summary>מבקשת ניתוח לשחקן שנבחר. תקלות מוצגות בטקסט המצב, והחלון נשאר פתוח.</summary>
    /// <param name="sender">כפתור הניתוח של החלון.</param>
    /// <param name="e">פרטי לחיצת הכפתור.</param>
    private async void Analyze_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeButton.IsEnabled = false;
        try
        {
            await _viewModel.AnalyzeSelectedPlayerAsync();
        }
        finally
        {
            AnalyzeButton.IsEnabled = true;
        }
    }
}
