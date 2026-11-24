// תפקיד הקובץ: מספק התראה משותפת על שינוי תכונה.
// שכבה: ViewModel של לקוח WPF.
// משתמשים: AiScoutViewModel ו־MatchesViewModel.
// קשר בפרויקט: מאפשר למסכים לרענן Binding כשמצב התצוגה משתנה.
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FootballManager.WPF.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
