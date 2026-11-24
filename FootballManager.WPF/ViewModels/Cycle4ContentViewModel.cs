// מטרה: מנהל מצב ופקודות העברות וסטטיסטיקות בסבב 4.
// מי משתמש: Cycle4Window.
// פעולות: RefreshTransfers, SaveTransfer ופקודות הסטטיסטיקות.
// תלויות: WcfServiceClient ו־AccountSessionViewModel.
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FootballManager.Contracts;
using FootballManager.WPF.Services;

namespace FootballManager.WPF.ViewModels;

/// <summary>ViewModel של חלון C4. הוא מחזיק נתוני טפסים ורשימות, בונה DTOs, מפעיל פקודות ומעביר בקשות ל־WCF עם אסימון המשתמש.</summary>
public sealed class Cycle4ContentViewModel : INotifyPropertyChanged
{
    private readonly WcfServiceClient _service;
    private readonly string _token;
    private readonly Func<string, bool> _confirmDelete;
    private readonly Action<TransferDto> _rememberTransfer;
    private IReadOnlyList<TransferDto> _transfers = [];
    private IReadOnlyList<PlayerStatisticDto> _statistics = [];
    private IReadOnlyList<PlayerDto> _players = [];
    private IReadOnlyList<TeamDto> _teams = [];
    private IReadOnlyList<MatchOption> _matchOptions = [];
    private TransferDto? _selectedTransfer;
    private PlayerStatisticDto? _selectedStatistic;
    private int _transferPlayerId, _fromTeamId, _toTeamId, _statisticPlayerId, _statisticMatchId;
    private DateTime? _transferDate = DateTime.Today;
    private string _price = "0", _goals = "0", _assists = "0", _minutes = "90", _rating = "8.5", _status = "";
    private bool _busy;

    public event PropertyChangedEventHandler? PropertyChanged;
    public string AccountLabel { get; }
    public bool Ready => !_busy;
    public IReadOnlyList<PlayerDto> Players { get => _players; private set => Set(ref _players, value); }
    public IReadOnlyList<TeamDto> Teams { get => _teams; private set => Set(ref _teams, value); }
    public IReadOnlyList<MatchOption> MatchOptions { get => _matchOptions; private set => Set(ref _matchOptions, value); }
    public IReadOnlyList<TransferDto> Transfers { get => _transfers; private set => Set(ref _transfers, value); }
    public IReadOnlyList<PlayerStatisticDto> Statistics { get => _statistics; private set => Set(ref _statistics, value); }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string TransferEditorTitle => SelectedTransfer is null ? "העברה חדשה" : $"עריכת העברה {SelectedTransfer.TransferId}";
    public string StatisticEditorTitle => SelectedStatistic is null ? "סטטיסטיקה חדשה" : $"עריכת סטטיסטיקה {SelectedStatistic.StatisticId}";

    public TransferDto? SelectedTransfer
    {
        get => _selectedTransfer;
        set
        {
            if (Set(ref _selectedTransfer, value)) PopulateTransferEditor();
        }
    }
    public PlayerStatisticDto? SelectedStatistic
    {
        get => _selectedStatistic;
        set
        {
            if (Set(ref _selectedStatistic, value)) PopulateStatisticEditor();
        }
    }
    public int TransferPlayerId { get => _transferPlayerId; set => Set(ref _transferPlayerId, value); }
    public int FromTeamId { get => _fromTeamId; set => Set(ref _fromTeamId, value); }
    public int ToTeamId { get => _toTeamId; set => Set(ref _toTeamId, value); }
    public DateTime? TransferDate { get => _transferDate; set => Set(ref _transferDate, value); }
    public string Price { get => _price; set => Set(ref _price, value); }
    public int StatisticPlayerId { get => _statisticPlayerId; set => Set(ref _statisticPlayerId, value); }
    public int StatisticMatchId { get => _statisticMatchId; set => Set(ref _statisticMatchId, value); }
    public string Goals { get => _goals; set => Set(ref _goals, value); }
    public string Assists { get => _assists; set => Set(ref _assists, value); }
    public string MinutesPlayed { get => _minutes; set => Set(ref _minutes, value); }
    public string Rating { get => _rating; set => Set(ref _rating, value); }

    public ICommand RefreshTransfers { get; }
    public ICommand NewTransfer { get; }
    public ICommand SaveTransfer { get; }
    public ICommand DeleteTransfer { get; }
    public ICommand RefreshStatistics { get; }
    public ICommand NewStatistic { get; }
    public ICommand SaveStatistic { get; }
    public ICommand DeleteStatistic { get; }

    /// <summary>מבצעת את שלב המערכת Cycle4Content View Model באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="service">ערך מסוג WcfServiceClient שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="session">ערך מסוג AccountSessionViewModel שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="confirmDelete">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <param name="rememberTransfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    public Cycle4ContentViewModel(
        WcfServiceClient service, AccountSessionViewModel session,
        Func<string, bool> confirmDelete, Action<TransferDto> rememberTransfer)
    {
        _service = service;
        _token = session.SessionToken;
        _confirmDelete = confirmDelete;
        _rememberTransfer = rememberTransfer;
        AccountLabel = $"{session.Username}  ·  מאמן";
        RefreshTransfers = Action(async () => { await LoadTransfersAsync(SelectedTransfer?.TransferId); });
        NewTransfer = Action(() =>
        {
            SelectedTransfer = null;
            PopulateTransferEditor();
            Status = "בחר שחקן, קבוצות ותאריך והזן מחיר.";
            return Task.CompletedTask;
        });
        SaveTransfer = Action(SaveTransferAsync);
        DeleteTransfer = Action(DeleteTransferAsync, () => SelectedTransfer is not null);
        RefreshStatistics = Action(async () => { await LoadStatisticsAsync(SelectedStatistic?.StatisticId); });
        NewStatistic = Action(() =>
        {
            SelectedStatistic = null;
            PopulateStatisticEditor();
            Status = "בחר שחקן ומשחק והזן נתוני משחק.";
            return Task.CompletedTask;
        });
        SaveStatistic = Action(SaveStatisticAsync);
        DeleteStatistic = Action(DeleteStatisticAsync, () => SelectedStatistic is not null);
    }

    /// <summary>נקראת כשהחלון נפתח. טוענת שחקנים, קבוצות ומשחקים כדי למלא בחירות, ואז טוענת העברות וסטטיסטיקות דרך WCF. אם טעינת נתוני הבסיס נכשלת, הודעת השירות מוצגת והטעינה נעצרת.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task InitializeAsync() => RunAsync(async () =>
    {
        var players = await _service.GetPlayersAsync();
        var teams = await _service.GetTeamsAsync();
        var matches = await _service.GetMatchesAsync(_token);
        if (!players.Succeeded || !teams.Succeeded || !matches.Succeeded)
        {
            Status = $"{players.Message} {teams.Message} {matches.Message}".Trim();
            return;
        }

        Players = players.Players;
        Teams = teams.Teams;
        MatchOptions = matches.Matches.Select(match => new MatchOption(
            match.MatchId,
            $"{match.MatchDate:dd.MM.yyyy} · {match.HomeTeamName} - {match.AwayTeamName}")).ToList();
        await LoadTransfersAsync(null);
        await LoadStatisticsAsync(null);
    });

    /// <summary>מריצה את הפעולה Action לפי זרימת העבודה של הרכיב. נקראת בקוד מתוך Cycle4Content View Model, Matches View Model.</summary>
    /// <param name="operation">ערך מסוג Func&lt;Task&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="allowed">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>ערך מסוג ICommand שמשמש את הקוד הקורא.</returns>
    private ICommand Action(Func<Task> operation, Func<bool>? allowed = null) =>
        new Cycle4Command(() => RunAsync(operation), () => Ready && (allowed?.Invoke() ?? true));

    /// <summary>עוטפת פקודות אסינכרוניות כדי למנוע שתי פעולות במקביל, מעדכנת את Ready ומריצה את הבקשה. חריגות הופכות להודעת מצב למשתמש, ולבסוף מצב העבודה מתנקה גם במקרה של שגיאה.</summary>
    /// <param name="operation">ערך מסוג Func&lt;Task&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RunAsync(Func<Task> operation)
    {
        if (_busy) return;
        _busy = true;
        Changed(nameof(Ready));
        CommandManager.InvalidateRequerySuggested();
        try { await operation(); }
        catch (Exception exception) { Status = $"הפעולה לא הושלמה. {exception.GetBaseException().Message}"; }
        finally
        {
            _busy = false;
            Changed(nameof(Ready));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>מבקשת את רשימת ההעברות משירות WCF עם אסימון המשתמש, מעדכנת את הרשימה ואת הפריט הנבחר ומציגה הודעת הצלחה או כשל. selectedId משמש לשמירת הבחירה לאחר רענון.</summary>
    /// <param name="selectedId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task LoadTransfersAsync(int? selectedId)
    {
        var result = await _service.GetTransfersAsync(_token);
        Transfers = result.Succeeded ? result.Transfers : [];
        SelectedTransfer = Transfers.FirstOrDefault(item => item.TransferId == selectedId) ?? Transfers.FirstOrDefault();
        if (SelectedTransfer is null) PopulateTransferEditor();
        Status = result.Succeeded ? $"נטענו {Transfers.Count} העברות." : result.Message;
    }

    /// <summary>מבקשת את סטטיסטיקות המשחק משירות WCF, מעדכנת את הרשימה ואת הבחירה, ומציגה הודעת שירות במקרה של הצלחה או כשל. selectedId משמר בחירה לאחר טעינה חוזרת.</summary>
    /// <param name="selectedId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task LoadStatisticsAsync(int? selectedId)
    {
        var result = await _service.GetPlayerStatisticsAsync(_token);
        Statistics = result.Succeeded ? result.Statistics : [];
        SelectedStatistic = Statistics.FirstOrDefault(item => item.StatisticId == selectedId) ?? Statistics.FirstOrDefault();
        if (SelectedStatistic is null) PopulateStatisticEditor();
        Status = result.Succeeded ? $"נטענו {Statistics.Count} רשומות סטטיסטיקה." : result.Message;
    }

    /// <summary>מבצעת את שלב המערכת Populate Transfer Editor באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך אתחול, Cycle4Content View Model.</summary>
    private void PopulateTransferEditor()
    {
        TransferPlayerId = SelectedTransfer?.PlayerId ?? Players.FirstOrDefault()?.PlayerId ?? 0;
        FromTeamId = SelectedTransfer?.FromTeamId ?? Teams.FirstOrDefault()?.TeamId ?? 0;
        ToTeamId = SelectedTransfer?.ToTeamId ?? Teams.Skip(1).FirstOrDefault()?.TeamId ?? 0;
        TransferDate = SelectedTransfer?.TransferDate.Date ?? DateTime.Today;
        Price = SelectedTransfer?.Price.ToString(CultureInfo.CurrentCulture) ?? "0";
        Changed(nameof(TransferEditorTitle));
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>מבצעת את שלב המערכת Populate Statistic Editor באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך אתחול, Cycle4Content View Model.</summary>
    private void PopulateStatisticEditor()
    {
        StatisticPlayerId = SelectedStatistic?.PlayerId ?? Players.FirstOrDefault()?.PlayerId ?? 0;
        StatisticMatchId = SelectedStatistic?.MatchId ?? MatchOptions.FirstOrDefault()?.MatchId ?? 0;
        Goals = SelectedStatistic?.Goals.ToString() ?? "0";
        Assists = SelectedStatistic?.Assists.ToString() ?? "0";
        MinutesPlayed = SelectedStatistic?.MinutesPlayed.ToString() ?? "90";
        Rating = SelectedStatistic?.Rating.ToString(CultureInfo.CurrentCulture) ?? "8.5";
        Changed(nameof(StatisticEditorTitle));
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>מופעלת בפקודת שמירה. בודקת בשכבת הממשק מזהים, מחיר ותאריך, בונה TransferDto ושולחת אותו ל־WCF עם אסימון ההפעלה. השרת מבצע בדיקת הרשאה וקלט נוספת. אחרי הצלחה הרשימה נטענת מחדש כדי להציג את הנתון שנשמר.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task SaveTransferAsync()
    {
        // בדיקה מוקדמת נותנת למשתמש הודעה ברורה; היא אינה מחליפה את בדיקת השרת.
        if (TransferPlayerId <= 0 || FromTeamId <= 0 || ToTeamId <= 0 || FromTeamId == ToTeamId)
        {
            Status = "יש לבחור שחקן ושתי קבוצות שונות.";
            return;
        }
        if (TransferDate is null || !decimal.TryParse(Price, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) ||
            price < 0)
        {
            Status = "יש לבחור תאריך ולהזין מחיר שאינו שלילי.";
            return;
        }

        var transfer = new TransferDto
        {
            TransferId = SelectedTransfer?.TransferId ?? 0,
            PlayerId = TransferPlayerId,
            FromTeamId = FromTeamId,
            ToTeamId = ToTeamId,
            TransferDate = TransferDate.Value.Date,
            Price = price
        };
        // המסך שולח את ה־DTO והאסימון לשירות. רק השרת ניגש למסד Access.
        var result = await _service.SaveTransferAsync(_token, transfer);
        Status = result.Message;
        if (result.Succeeded)
        {
            _rememberTransfer(transfer);
            await LoadTransfersAsync(transfer.TransferId);
            Status = result.Message;
        }
    }

    /// <summary>מופעלת מפקודת המחיקה. דורשת בחירה ואישור משתמש, שולחת את המזהה והאסימון ל־WCF, ומרעננת את הרשימה רק לאחר הצלחה. שגיאת השירות מוצגת ב־Status.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task DeleteTransferAsync()
    {
        if (SelectedTransfer is not { } transfer ||
            !_confirmDelete($"העברת {transfer.PlayerName} מ־{transfer.FromTeamName} ל־{transfer.ToTeamName}"))
            return;

        var result = await _service.DeleteTransferAsync(_token, transfer.TransferId);
        Status = result.Message;
        if (result.Succeeded)
        {
            await LoadTransfersAsync(null);
            Status = result.Message;
        }
    }

    /// <summary>מופעלת מפקודת שמירת סטטיסטיקה. בודקת מזהי שחקן ומשחק, ממירה שערים, בישולים, דקות ודירוג מטקסט, בונה PlayerStatisticDto ושולחת ל־WCF. השרת בודק שוב הרשאה ותקינות; הצלחה גורמת לטעינה מחדש.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task SaveStatisticAsync()
    {
        if (StatisticPlayerId <= 0 || StatisticMatchId <= 0 ||
            !int.TryParse(Goals, out var goals) || goals is < 0 or > 32767 ||
            !int.TryParse(Assists, out var assists) || assists is < 0 or > 32767 ||
            !int.TryParse(MinutesPlayed, out var minutes) || minutes is < 0 or > 32767 ||
            !float.TryParse(Rating, NumberStyles.Float, CultureInfo.CurrentCulture, out var rating) ||
            !float.IsFinite(rating) || rating is < 0 or > 100)
        {
            Status = "יש לבחור שחקן ומשחק ולהזין שערים, בישולים ודקות בין 0 ל־32767 ודירוג בין 0 ל־100.";
            return;
        }

        var statistic = new PlayerStatisticDto
        {
            StatisticId = SelectedStatistic?.StatisticId ?? 0,
            PlayerId = StatisticPlayerId,
            MatchId = StatisticMatchId,
            Goals = goals,
            Assists = assists,
            MinutesPlayed = minutes,
            Rating = rating
        };
        // נתוני הטופס עוברים כחוזה משותף; ה־ViewModel אינו מבצע SQL.
        var result = await _service.SavePlayerStatisticAsync(_token, statistic);
        Status = result.Message;
        if (result.Succeeded)
        {
            await LoadStatisticsAsync(statistic.StatisticId);
            Status = result.Message;
        }
    }

    /// <summary>מופעלת מפקודת מחיקת סטטיסטיקה. מבקשת אישור, שולחת מזהה ואסימון ל־WCF, ומרעננת לאחר הצלחה; במקרה כשל היא משאירה את ההודעה שהחזיר השירות.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task DeleteStatisticAsync()
    {
        if (SelectedStatistic is not { } statistic ||
            !_confirmDelete($"סטטיסטיקה {statistic.StatisticId} של {statistic.PlayerName}"))
            return;

        var result = await _service.DeletePlayerStatisticAsync(_token, statistic.StatisticId);
        Status = result.Message;
        if (result.Succeeded)
        {
            await LoadStatisticsAsync(null);
            Status = result.Message;
        }
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
        Changed(name);
        return true;
    }
    /// <summary>מבצעת את שלב המערכת Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Run Async, Populate Transfer Editor.</summary>
    /// <param name="name">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    private void Changed(string name) => PropertyChanged?.Invoke(this, new(name));

    /// <summary>מימוש ICommand שמחבר פקודות Binding ב־WPF לפעולות האסינכרוניות של Cycle4ContentViewModel ומאפשר לבדוק אם ניתן להריץ אותן.</summary>
    private sealed class Cycle4Command(Func<Task> execute, Func<bool> canExecute) : ICommand
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

/// <summary>המחלקה MatchOption מרכזת אחריות של ViewModel של WPF. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
public sealed record MatchOption(int MatchId, string Label);
