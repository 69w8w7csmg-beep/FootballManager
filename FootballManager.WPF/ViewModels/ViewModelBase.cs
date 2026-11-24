// מטרה: מספק הודעת שינוי משותפת לתצוגות WPF.
// מי משתמש: MatchesViewModel ו־AiScoutViewModel.
// פעולות: OnPropertyChanged.
// תלויות: INotifyPropertyChanged ו־Binding.
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FootballManager.WPF.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>מודיע ל־Binding איזה מאפיין צריך לרענן.</summary>
    /// <param name="name">שם המאפיין, או הערך שמתקבל מהשדה בהתאם לפעולה.</param>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
