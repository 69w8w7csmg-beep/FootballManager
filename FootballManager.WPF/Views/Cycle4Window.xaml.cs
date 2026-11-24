// מטרה: מקים חלון C4 ומחבר אותו לתצוגה ולפקודות.
// מי משתמש: MainWindow ו־Cycle4Window.xaml.
// פעולות: אתחול, אירועי בחירה וסגירת החלון.
// תלויות: Cycle4ContentViewModel.
using System.Windows;
using FootballManager.Contracts;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

/// <summary>חלון WPF שמציג למאמן את שתי יכולות C4: ניהול העברות וניהול סטטיסטיקות. פקודות Binding מחוברות ל־Cycle4ContentViewModel.</summary>
public partial class Cycle4Window : Window
{
    /// <summary>מבצעת את שלב המערכת Cycle4Window באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="service">ערך מסוג WcfServiceClient שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="session">ערך מסוג AccountSessionViewModel שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="rememberTransfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    public Cycle4Window(
        WcfServiceClient service, AccountSessionViewModel session, Action<TransferDto> rememberTransfer)
    {
        InitializeComponent();
        var model = new Cycle4ContentViewModel(service, session, ConfirmDelete, rememberTransfer);
        DataContext = model;
        Loaded += async (_, _) => await model.InitializeAsync();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Close_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>מבצעת את שלב המערכת Confirm Delete באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="description">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    private bool ConfirmDelete(string description) =>
        MessageBox.Show(this, $"למחוק את {description}?", "אישור מחיקה",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) == MessageBoxResult.Yes;
}
