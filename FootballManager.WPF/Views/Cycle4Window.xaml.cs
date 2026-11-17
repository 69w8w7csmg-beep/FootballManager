// חלון C4 מציג למאמן את פקודות ההעברות והסטטיסטיקות מתוך ה־ViewModel.
using System.Windows;
using FootballManager.Contracts;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

public partial class Cycle4Window : Window
{
    public Cycle4Window(
        WcfServiceClient service, AccountSessionViewModel session, Action<TransferDto> rememberTransfer)
    {
        InitializeComponent();
        var model = new Cycle4ContentViewModel(service, session, ConfirmDelete, rememberTransfer);
        DataContext = model;
        Loaded += async (_, _) => await model.InitializeAsync();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private bool ConfirmDelete(string description) =>
        MessageBox.Show(this, $"למחוק את {description}?", "אישור מחיקה",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) == MessageBoxResult.Yes;
}
