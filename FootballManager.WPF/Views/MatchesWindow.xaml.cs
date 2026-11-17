// חלון המשחקים מציג נתונים ופקודות שמגיעים מ־MatchesViewModel.
using System.Windows;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

/// <summary>חלון WPF למשחקים ותוצאות C3. הוא מציג נתוני MatchesViewModel ומשתמש בפקודות ה־ViewModel במקום SQL.</summary>
public partial class MatchesWindow : Window
{
    /// <summary>מבצעת את שלב המערכת Matches Window באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="service">ערך מסוג WcfServiceClient שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="session">ערך מסוג AccountSessionViewModel שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public MatchesWindow(WcfServiceClient service, AccountSessionViewModel session)
    {
        InitializeComponent();
        var model = new MatchesViewModel(service, session, ConfirmDelete);
        DataContext = model;
        Loaded += async (_, _) => await model.InitializeAsync();
    }

    /// <summary>מבצעת את שלב המערכת Confirm Delete באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="description">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    private bool ConfirmDelete(string description) =>
        MessageBox.Show(this, $"למחוק את המשחק {description}?", "מחיקת משחק",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) == MessageBoxResult.Yes;
}
