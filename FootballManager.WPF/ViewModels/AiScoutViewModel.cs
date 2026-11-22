using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FootballManager.Contracts;
using FootballManager.WPF.Services;

namespace FootballManager.WPF.ViewModels;

/// <summary>ViewModel לחלון הסקאוט. הוא טוען שחקנים ומבקש ניתוח דרך WcfServiceClient, בלי חיבור למסד Access.</summary>
public sealed class AiScoutViewModel : INotifyPropertyChanged
{
    private readonly WcfServiceClient _serviceClient;
    private readonly AccountSessionViewModel _session;
    private PlayerDto? _selectedPlayer;
    private string _analysisText = string.Empty;
    private string _statusText = "טוען שחקנים…";
    private bool _isBusy;

    /// <summary>יוצרת את ה־ViewModel עם מתאם WCF ועם Session שהתקבל מהשרת.</summary>
    /// <param name="serviceClient">מתאם הקריאות לחוזה WCF.</param>
    /// <param name="session">פרטי המשתמש ואסימון ההתחברות שקיבל הלקוח.</param>
    public AiScoutViewModel(WcfServiceClient serviceClient, AccountSessionViewModel session)
    {
        _serviceClient = serviceClient;
        _session = session;
    }

    /// <summary>רשימת השחקנים הציבוריים שמגיעה משירות WCF.</summary>
    public ObservableCollection<PlayerDto> Players { get; } = [];

    /// <summary>השחקן שנבחר ברשימה.</summary>
    public PlayerDto? SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (ReferenceEquals(_selectedPlayer, value)) return;
            _selectedPlayer = value;
            AnalysisText = string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>הניתוח שהתקבל מ־WCF להצגה בחלון.</summary>
    public string AnalysisText
    {
        get => _analysisText;
        private set { _analysisText = value; OnPropertyChanged(); }
    }

    /// <summary>הודעת הצלחה, חסימה או תקלה לתלמיד ולמשתמש.</summary>
    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    /// <summary>מציין אם מתבצעת כרגע בקשה לשרת.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    /// <summary>טוענת את השחקנים הציבוריים דרך שירות WCF, ומציגה שגיאה בחלון אם השירות אינו זמין.</summary>
    public async Task LoadPlayersAsync()
    {
        try
        {
            var response = await _serviceClient.GetPlayersAsync();
            Players.Clear();
            if (response.Succeeded)
            {
                foreach (var player in response.Players) Players.Add(player);
                SelectedPlayer = Players.FirstOrDefault();
                StatusText = Players.Count > 0 ? "בחר שחקן לניתוח." : "לא נמצאו שחקנים במסד.";
            }
            else
            {
                StatusText = response.Message;
            }
        }
        catch (Exception)
        {
            StatusText = "לא ניתן לטעון שחקנים. בדוק ששירות WCF ומסד Access פועלים.";
        }
    }

    /// <summary>מבקש מהשרת לנתח את השחקן שנבחר. שגיאת הרשאה או תקשורת מוצגת בחלון והוא נשאר פתוח.</summary>
    public async Task AnalyzeSelectedPlayerAsync()
    {
        if (IsBusy) return;
        if (SelectedPlayer is null)
        {
            StatusText = "יש לבחור שחקן לפני הניתוח.";
            return;
        }

        IsBusy = true;
        AnalysisText = string.Empty;
        StatusText = "שולח בקשת ניתוח לשרת…";
        try
        {
            var response = await _serviceClient.AnalyzePlayerAsync(_session.SessionToken, SelectedPlayer.PlayerId);
            StatusText = response.Message;
            AnalysisText = response.Succeeded ? response.Analysis : string.Empty;
        }
        catch (Exception)
        {
            StatusText = "לא ניתן לקבל ניתוח כרגע. בדוק את שירות WCF, הגדרות השרת והחיבור.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>מודיעה ל־WPF לרענן Binding לאחר שינוי ערך.</summary>
    /// <param name="propertyName">שם המאפיין שהשתנה.</param>
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>אירוע Binding של WPF לשינויים במאפייני ה־ViewModel.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
