// פקודות ותצוגת משחקים ב־WPF. הגישה לנתונים מתבצעת דרך לקוח שירות WCF.
using System.Windows.Input;
using System.Runtime.CompilerServices;
using FootballManager.Contracts;
using FootballManager.WPF.Services;

namespace FootballManager.WPF.ViewModels;

/// <summary>ViewModel של מסך משחקי C3. הוא מנהל חיפוש, בחירה ופקודות CRUD ומעביר את פעולות המשחקים ל־WCF.</summary>
public sealed class MatchesViewModel : ViewModelBase
{
    private readonly WcfServiceClient _service;
    private readonly string _token;
    private readonly Func<string, bool> _confirmDelete;
    private IReadOnlyList<MatchDto> _matches = [];
    private IReadOnlyList<TeamDto> _teams = [];
    private MatchDto? _selectedMatch;
    private int _homeTeamId, _awayTeamId;
    private DateTime? _matchDate = DateTime.Today;
    private string _homeScore = "0", _awayScore = "0", _status = "";
    private bool _busy;

    public bool IsManager { get; }
    public bool IsPlayer { get; }
    public string AccountLabel { get; }
    public bool Ready => !_busy;
    public string SearchText { get; set; } = "";
    public DateTime? FilterDate { get; set; }
    public string EditorTitle => SelectedMatch is null ? "משחק חדש" : $"עריכת משחק {SelectedMatch.MatchId}";
    public int HomeTeamId { get => _homeTeamId; set => Set(ref _homeTeamId, value); }
    public int AwayTeamId { get => _awayTeamId; set => Set(ref _awayTeamId, value); }
    public DateTime? MatchDate { get => _matchDate; set => Set(ref _matchDate, value); }
    public string HomeScore { get => _homeScore; set => Set(ref _homeScore, value); }
    public string AwayScore { get => _awayScore; set => Set(ref _awayScore, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public IReadOnlyList<MatchDto> Matches { get => _matches; private set => Set(ref _matches, value); }
    public IReadOnlyList<TeamDto> Teams { get => _teams; private set => Set(ref _teams, value); }
    public MatchDto? SelectedMatch
    {
        get => _selectedMatch;
        set
        {
            if (Set(ref _selectedMatch, value)) PopulateEditor();
        }
    }
    public ICommand Refresh { get; }
    public ICommand ClearFilters { get; }
    public ICommand New { get; }
    public ICommand Save { get; }
    public ICommand Delete { get; }
    public ICommand TrySaveResult { get; }

    /// <summary>מבצעת את שלב המערכת Matches View Model באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="service">ערך מסוג WcfServiceClient שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="session">ערך מסוג AccountSessionViewModel שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="confirmDelete">דגל המציין אם המצב או הפעולה מופעלים.</param>
    public MatchesViewModel(WcfServiceClient service, AccountSessionViewModel session, Func<string, bool> confirmDelete)
    {
        _service = service;
        _token = session.SessionToken;
        _confirmDelete = confirmDelete;
        IsManager = session.Role == "Manager";
        IsPlayer = session.Role == "Player";
        AccountLabel = $"{session.Username}  ·  {(IsManager ? "מאמן" : "שחקן")}";
        Refresh = Action(async () => { await LoadMatchesAsync(SelectedMatch?.MatchId); });
        ClearFilters = Action(async () =>
        {
            SearchText = "";
            FilterDate = null;
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(FilterDate));
            await LoadMatchesAsync(SelectedMatch?.MatchId);
        });
        New = Action(() =>
        {
            SelectedMatch = null;
            PopulateEditor();
            Status = "בחרו קבוצות ותאריך והזינו את התוצאה.";
            return Task.CompletedTask;
        }, () => IsManager);
        Save = Action(SaveAsync, () => IsManager);
        Delete = Action(DeleteAsync, () => IsManager && SelectedMatch != null);
        TrySaveResult = Action(TrySaveResultAsync, () => IsPlayer && SelectedMatch != null);
    }

    /// <summary>מכינה או מאתחלת את רכיב המערכת המתואר בשם Initialize Async לפני השימוש בו. בגוף הפעולה: פונה לשירות דרך שכבת WCF. נקראת בקוד מתוך Cycle4Window, Matches Window.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task InitializeAsync() => RunAsync(async () =>
    {
        if (IsManager)
        {
            var teams = await _service.GetTeamsAsync();
            if (!teams.Succeeded) { Status = teams.Message; return; }
            Teams = teams.Teams;
        }
        await LoadMatchesAsync(null);
    });

    /// <summary>מריצה את הפעולה Action לפי זרימת העבודה של הרכיב. נקראת בקוד מתוך Cycle4Content View Model, Matches View Model.</summary>
    /// <param name="operation">ערך מסוג Func&lt;Task&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="allowed">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>ערך מסוג ICommand שמשמש את הקוד הקורא.</returns>
    private ICommand Action(Func<Task> operation, Func<bool>? allowed = null) =>
        new MatchCommand(() => RunAsync(operation), () => Ready && (allowed?.Invoke() ?? true));

    /// <summary>מריצה את הפעולה Run Async לפי זרימת העבודה של הרכיב. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Initialize Async, Action.</summary>
    /// <param name="operation">ערך מסוג Func&lt;Task&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RunAsync(Func<Task> operation)
    {
        if (_busy) return;
        _busy = true;
        OnPropertyChanged(nameof(Ready));
        CommandManager.InvalidateRequerySuggested();
        try { await operation(); }
        catch (Exception ex) { Status = $"הפעולה לא הושלמה. {ex.GetBaseException().Message}"; }
        finally
        {
            _busy = false;
            OnPropertyChanged(nameof(Ready));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Load Matches Async ומחזירה את התוצאה לשכבה הקוראת. בגוף הפעולה: פונה לשירות דרך שכבת WCF. נקראת בקוד מתוך Matches View Model, Initialize Async.</summary>
    /// <param name="selectId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task<bool> LoadMatchesAsync(int? selectId)
    {
        var result = await _service.GetMatchesAsync(_token, SearchText.Trim(), FilterDate?.Date);
        Matches = result.Succeeded ? result.Matches : [];
        SelectedMatch = Matches.FirstOrDefault(m => m.MatchId == selectId) ?? Matches.FirstOrDefault();
        if (SelectedMatch == null) PopulateEditor();
        Status = !result.Succeeded ? result.Message : Matches.Count == 0 ? "לא נמצאו משחקים." : $"נטענו {Matches.Count} משחקים.";
        return result.Succeeded;
    }

    /// <summary>מבצעת את שלב המערכת Populate Editor באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך אתחול, Matches View Model.</summary>
    private void PopulateEditor()
    {
        HomeTeamId = SelectedMatch?.HomeTeamId ?? Teams.FirstOrDefault()?.TeamId ?? 0;
        AwayTeamId = SelectedMatch?.AwayTeamId ?? Teams.Skip(1).FirstOrDefault()?.TeamId ?? 0;
        MatchDate = SelectedMatch?.MatchDate.Date ?? DateTime.Today;
        HomeScore = SelectedMatch?.HomeScore.ToString() ?? "0";
        AwayScore = SelectedMatch?.AwayScore.ToString() ?? "0";
        OnPropertyChanged(nameof(EditorTitle));
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>מבצעת את פעולת הנתונים Save Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: פונה לשירות דרך שכבת WCF.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task SaveAsync()
    {
        if (HomeTeamId <= 0 || AwayTeamId <= 0 || HomeTeamId == AwayTeamId)
        {
            Status = "יש לבחור שתי קבוצות שונות.";
            return;
        }
        if (MatchDate is null || !int.TryParse(HomeScore, out int home) || home < 0 ||
            !int.TryParse(AwayScore, out int away) || away < 0)
        {
            Status = "יש לבחור תאריך ולהזין לכל קבוצה מספר שערים שלם שאינו שלילי.";
            return;
        }
        var game = new MatchDto
        {
            MatchId = SelectedMatch?.MatchId ?? 0, HomeTeamId = HomeTeamId, AwayTeamId = AwayTeamId,
            MatchDate = MatchDate.Value.Date + (SelectedMatch?.MatchDate.TimeOfDay ?? TimeSpan.Zero),
            HomeScore = home, AwayScore = away
        };
        var result = await _service.SaveMatchAsync(_token, game);
        Status = result.Message;
        if (result.Succeeded && await LoadMatchesAsync(game.MatchId))
            Status = result.Message;
    }

    /// <summary>מבצעת את פעולת הנתונים Delete Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: פונה לשירות דרך שכבת WCF.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task DeleteAsync()
    {
        if (SelectedMatch is not { } game || !_confirmDelete($"{game.HomeTeamName} מול {game.AwayTeamName} ({game.MatchDate:dd/MM/yyyy})"))
            return;
        var result = await _service.DeleteMatchAsync(_token, game.MatchId);
        Status = result.Message;
        if (result.Succeeded && await LoadMatchesAsync(null))
            Status = result.Message;
    }

    /// <summary>מנסה לבצע את הפעולה Try Save Result Async ומחזירה תוצאה שמאפשרת לקוד הקורא לזהות הצלחה או כישלון. בגוף הפעולה: פונה לשירות דרך שכבת WCF.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task TrySaveResultAsync()
    {
        if (SelectedMatch is not { } game) return;
        var result = await _service.SaveMatchAsync(_token, game);
        Status = result.Message;
    }

    /// <summary>מבצעת את פעולת הנתונים Set. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך אתחול.</summary>
    /// <param name="field">ערך מסוג T שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="value">ערך מסוג T שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="name">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    private bool Set<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
    /// <summary>מבצעת את שלב המערכת Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Run Async, Populate Transfer Editor.</summary>
    /// <param name="name">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>

    /// <summary>מימוש ICommand שמחבר פקודות מסך המשחקים לפעולות MatchesViewModel ובודק אם הפעולה זמינה.</summary>
    private sealed class MatchCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        /// <summary>בודקת את התנאי Can Execute ומחזירה תשובת כן או לא לשימוש בקוד הקורא או בממשק. נקראת בקוד מתוך Execute.</summary>
        /// <param name="parameter">ערך מסוג object? שהפעולה צריכה כדי לבצע את תפקידה.</param>
        /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
        public bool CanExecute(object? parameter) => canExecute();
        /// <summary>מריצה את הפעולה Execute לפי זרימת העבודה של הרכיב.</summary>
        /// <param name="parameter">ערך מסוג object? שהפעולה צריכה כדי לבצע את תפקידה.</param>
        public async void Execute(object? parameter) { if (CanExecute(parameter)) await execute(); }
    }
}
