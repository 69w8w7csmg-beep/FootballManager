// חלון המשחקים מציג נתונים ופקודות שמגיעים מ־MatchesViewModel.
using System.Windows;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

public partial class MatchesWindow : Window
{
    public MatchesWindow(WcfServiceClient service, AccountSessionViewModel session)
    {
        InitializeComponent();
        var model = new MatchesViewModel(service, session, ConfirmDelete);
        DataContext = model;
        Loaded += async (_, _) => await model.InitializeAsync();
    }

    private bool ConfirmDelete(string description) =>
        MessageBox.Show(this, $"למחוק את המשחק {description}?", "מחיקת משחק",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) == MessageBoxResult.Yes;
}
