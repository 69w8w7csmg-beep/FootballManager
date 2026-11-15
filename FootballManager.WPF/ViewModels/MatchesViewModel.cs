// פקודות ותצוגת משחקים ב־WPF. הגישה לנתונים מתבצעת דרך לקוח שירות WCF.
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FootballManager.Contracts;
using FootballManager.WPF.Services;

namespace FootballManager.WPF.ViewModels;

public sealed class MatchesViewModel : INotifyPropertyChanged
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

    public event PropertyChangedEventHandler? PropertyChanged;
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
            Changed(nameof(SearchText));
            Changed(nameof(FilterDate));
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

    private ICommand Action(Func<Task> operation, Func<bool>? allowed = null) =>
        new MatchCommand(() => RunAsync(operation), () => Ready && (allowed?.Invoke() ?? true));

    private async Task RunAsync(Func<Task> operation)
    {
        if (_busy) return;
        _busy = true;
        Changed(nameof(Ready));
        CommandManager.InvalidateRequerySuggested();
        try { await operation(); }
        catch (Exception ex) { Status = $"הפעולה לא הושלמה. {ex.GetBaseException().Message}"; }
        finally
        {
            _busy = false;
            Changed(nameof(Ready));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task<bool> LoadMatchesAsync(int? selectId)
    {
        var result = await _service.GetMatchesAsync(_token, SearchText.Trim(), FilterDate?.Date);
        Matches = result.Succeeded ? result.Matches : [];
        SelectedMatch = Matches.FirstOrDefault(m => m.MatchId == selectId) ?? Matches.FirstOrDefault();
        if (SelectedMatch == null) PopulateEditor();
        Status = !result.Succeeded ? result.Message : Matches.Count == 0 ? "לא נמצאו משחקים." : $"נטענו {Matches.Count} משחקים.";
        return result.Succeeded;
    }

    private void PopulateEditor()
    {
        HomeTeamId = SelectedMatch?.HomeTeamId ?? Teams.FirstOrDefault()?.TeamId ?? 0;
        AwayTeamId = SelectedMatch?.AwayTeamId ?? Teams.Skip(1).FirstOrDefault()?.TeamId ?? 0;
        MatchDate = SelectedMatch?.MatchDate.Date ?? DateTime.Today;
        HomeScore = SelectedMatch?.HomeScore.ToString() ?? "0";
        AwayScore = SelectedMatch?.AwayScore.ToString() ?? "0";
        Changed(nameof(EditorTitle));
        CommandManager.InvalidateRequerySuggested();
    }

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

    private async Task DeleteAsync()
    {
        if (SelectedMatch is not { } game || !_confirmDelete($"{game.HomeTeamName} מול {game.AwayTeamName} ({game.MatchDate:dd/MM/yyyy})"))
            return;
        var result = await _service.DeleteMatchAsync(_token, game.MatchId);
        Status = result.Message;
        if (result.Succeeded && await LoadMatchesAsync(null))
            Status = result.Message;
    }

    private async Task TrySaveResultAsync()
    {
        if (SelectedMatch is not { } game) return;
        var result = await _service.SaveMatchAsync(_token, game);
        Status = result.Message;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Changed(name);
        return true;
    }
    private void Changed(string name) => PropertyChanged?.Invoke(this, new(name));

    private sealed class MatchCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        public bool CanExecute(object? parameter) => canExecute();
        public async void Execute(object? parameter) { if (CanExecute(parameter)) await execute(); }
    }
}
