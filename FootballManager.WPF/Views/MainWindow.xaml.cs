// טיפול בתצוגה ובאירועי החלון הראשי. בדיקות טפסי החשבון נמצאות ב־ViewModel.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FootballManager.Contracts;
using FootballManager.WPF.Models;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

/// <summary>חלון WPF הראשי. הוא מנהל כניסה, הרשמה, דשבורד ותוכן קיים, ומעביר בקשות ל־WcfServiceClient ולחלונות ViewModel.</summary>
public partial class MainWindow : Window
{
    private readonly WcfServiceClient _serviceClient = new();
    private readonly AccountSessionViewModel _session = new();
    private readonly AuthenticationViewModel _authentication = new();
    private readonly GuestMatchesViewModel _guestMatches = new();
    private TransferDto? _lastSavedTransfer;
    private IReadOnlyList<ManagedUserDto> _cachedManagedUsers = [];
    private bool _syncingLoginPassword;
    private bool _syncingRegisterPassword;
    private bool _syncingRegisterConfirmation;
    private string _sessionToken => _session.SessionToken;
    private string _currentRole => _session.Role;
    private string _currentUsername => _session.Username;

    /// <summary>מבצעת את שלב המערכת Main Window באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    public MainWindow()
    {
        InitializeComponent();
        ApplyMatchFilter("All");
        UpdateLoginValidation();
        UpdateRegistrationValidation();
    }

    private string LoginPassword =>
        LoginPasswordTextBox.Visibility == Visibility.Visible
            ? LoginPasswordTextBox.Text
            : LoginPasswordBox.Password;

    private string RegistrationPassword =>
        RegisterPasswordTextBox.Visibility == Visibility.Visible
            ? RegisterPasswordTextBox.Text
            : RegisterPasswordBox.Password;

    private string RegistrationConfirmation =>
        RegisterConfirmPasswordTextBox.Visibility == Visibility.Visible
            ? RegisterConfirmPasswordTextBox.Text
            : RegisterConfirmPasswordBox.Password;

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Open Login_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void OpenLogin_Click(object sender, RoutedEventArgs e) => OpenAuthentication(0);

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Open Matches_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך. נקראת בקוד מתוך Show Matches_Click.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void OpenMatches_Click(object sender, RoutedEventArgs e) =>
        new MatchesWindow(_serviceClient, _session) { Owner = this }.ShowDialog();

    /// <summary>מופעלת מכפתור C4 בדשבורד. בודקת את התפקיד המקומי כדי להציג את החלון רק למאמן, יוצרת את ה־ViewModel ופותחת את מסך ההעברות והסטטיסטיקות. השרת בודק הרשאות שוב בכל בקשה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void OpenCycle4_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Manager") return;
        new Cycle4Window(_serviceClient, _session, RememberSavedTransfer) { Owner = this }.ShowDialog();
    }

    /// <summary>פותח את כלי הסקאוט אחרי התחברות. כל משתמש רשאי להגיע למסך לצורך הדגמת בדיקת ההרשאה; רק השרת מרשה בפועל את הפעולה ל־Player.</summary>
    /// <param name="sender">כפתור סקאוט ה־AI בדשבורד.</param>
    /// <param name="e">פרטי אירוע הלחיצה.</param>
    private void OpenAiScout_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_sessionToken)) return;
        new AiScoutWindow(_serviceClient, _session) { Owner = this }.ShowDialog();
    }

    /// <summary>פותחת את חלון הצ'אט. השרת יבדוק את התפקיד בכל בקשה.</summary>
    private void OpenAiChat_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_sessionToken)) return;
        new AiChatWindow(_serviceClient, _session) { Owner = this }.ShowDialog();
    }

    /// <summary>מבצעת את שלב המערכת Remember Saved Transfer באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    private void RememberSavedTransfer(TransferDto transfer)
    {
        // שומרים העתק של הבקשה האחרונה כדי להראות שחסימת השחקן חלה על אותה פעולה.
        _lastSavedTransfer = new TransferDto
        {
            TransferId = transfer.TransferId,
            PlayerId = transfer.PlayerId,
            FromTeamId = transfer.FromTeamId,
            ToTeamId = transfer.ToTeamId,
            TransferDate = transfer.TransferDate,
            Price = transfer.Price
        };
    }

    /// <summary>מופעלת מכפתור בדיקת הרשאה אצל Player. שולחת ל־WCF את אותה העברה שנשמרה קודם עם אסימון השחקן. השרת אמור לחסום ב־403; ההודעה מוצגת במסך וחריגת תקשורת מוצגת ככשל גישה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void PlayerTransferAttempt_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Player" || _lastSavedTransfer is null) return;
        PlayerTransferAttemptButton.IsEnabled = false;
        try
        {
            // שולחים את אותה בקשת שמירה כדי להדגים שאכיפת ההרשאה נמצאת בשרת.
            var result = await _serviceClient.SaveTransferAsync(_sessionToken, _lastSavedTransfer);
            PlayerTransferStatus.Text = result.Message;
        }
        catch (Exception exception)
        {
            PlayerTransferStatus.Text = $"לא הצלחתי לפנות לשירות. {exception.GetBaseException().Message}";
        }
        finally
        {
            PlayerTransferStatus.Visibility = Visibility.Visible;
            PlayerTransferAttemptButton.IsEnabled = true;
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Open Player Registration_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void OpenPlayerRegistration_Click(object sender, RoutedEventArgs e) => OpenRegistration();

    /// <summary>פותחת את המסך או המשאב Open Registration בעקבות פעולת המשתמש. נקראת בקוד מתוך Open Player Registration_Click.</summary>
    private void OpenRegistration()
    {
        OpenAuthentication(1);
        UpdateRegistrationValidation();
    }

    /// <summary>פותחת את המסך או המשאב Open Authentication בעקבות פעולת המשתמש. נקראת בקוד מתוך Open Login_Click, Open Registration.</summary>
    /// <param name="tabIndex">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    private void OpenAuthentication(int tabIndex)
    {
        GuestHomePanel.Visibility = Visibility.Collapsed;
        ContentBrowsePanel.Visibility = Visibility.Collapsed;
        DashboardPanel.Visibility = Visibility.Collapsed;
        AuthPanel.Visibility = Visibility.Visible;
        AuthTabs.SelectedIndex = tabIndex;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Show Guest_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ShowGuest_Click(object sender, RoutedEventArgs e)
    {
        AuthPanel.Visibility = Visibility.Collapsed;
        ContentBrowsePanel.Visibility = Visibility.Collapsed;
        var isLoggedIn = !string.IsNullOrEmpty(_sessionToken);
        DashboardPanel.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;
        GuestHomePanel.Visibility = isLoggedIn ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Show Players_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void ShowPlayers_Click(object sender, RoutedEventArgs e)
    {
        OpenContentBrowser(0);
        await RefreshPublicPlayersAsync();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Show Teams_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void ShowTeams_Click(object sender, RoutedEventArgs e)
    {
        OpenContentBrowser(1);
        await RefreshPublicTeamsAsync();
    }

    /// <summary>פותחת את המסך או המשאב Open Content Browser בעקבות פעולת המשתמש. נקראת בקוד מתוך Show Players_Click, Show Teams_Click.</summary>
    /// <param name="selectedTab">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    private void OpenContentBrowser(int selectedTab)
    {
        AuthPanel.Visibility = Visibility.Collapsed;
        GuestHomePanel.Visibility = Visibility.Collapsed;
        DashboardPanel.Visibility = Visibility.Collapsed;
        ContentBrowsePanel.Visibility = Visibility.Visible;
        FavoritesTab.Visibility = _currentRole == "Player" ? Visibility.Visible : Visibility.Collapsed;
        ManagerToolsTab.Visibility = _currentRole == "Manager" ? Visibility.Visible : Visibility.Collapsed;
        AddFavoriteButton.Visibility = _currentRole == "Player" ? Visibility.Visible : Visibility.Collapsed;
        ContentTabs.SelectedIndex = selectedTab;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Search Players_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void SearchPlayers_Click(object sender, RoutedEventArgs e) => await RefreshPublicPlayersAsync();

    /// <summary>מבצעת את שלב המערכת Refresh Public Players Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Show Players_Click, Search Players_Click.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RefreshPublicPlayersAsync()
    {
        try
        {
            var position = (PlayerPositionFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? string.Empty;
            var result = await _serviceClient.GetPlayersAsync(PlayerSearchTextBox.Text, position);
            PublicPlayersList.ItemsSource = result.Succeeded ? result.Players : null;
            ContentMessageText.Text = result.Message;
        }
        catch (Exception exception)
        {
            ContentMessageText.Text = $"לא הצלחתי לטעון שחקנים. ודא ש־WCF פועל. {exception.Message}";
        }
    }

    /// <summary>מבצעת את שלב המערכת Refresh Public Teams Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Show Teams_Click.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RefreshPublicTeamsAsync()
    {
        try
        {
            var result = await _serviceClient.GetTeamsAsync();
            PublicTeamsList.ItemsSource = result.Succeeded ? result.Teams : null;
            PublicRosterList.ItemsSource = null;
            ContentMessageText.Text = result.Message;
        }
        catch (Exception exception)
        {
            ContentMessageText.Text = $"לא הצלחתי לטעון קבוצות. ודא ש־WCF פועל. {exception.Message}";
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Public Teams List_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void PublicTeamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PublicTeamsList.SelectedItem is not TeamDto team)
        {
            PublicRosterList.ItemsSource = null;
            return;
        }

        try
        {
            var result = await _serviceClient.GetTeamRosterAsync(team.TeamId);
            PublicRosterList.ItemsSource = result.Succeeded ? result.Players : null;
            ContentMessageText.Text = result.Message;
        }
        catch (Exception exception)
        {
            ContentMessageText.Text = $"לא הצלחתי לטעון את הסגל. {exception.Message}";
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Public Players List_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void PublicPlayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AddFavoriteButton.IsEnabled = _currentRole == "Player" && PublicPlayersList.SelectedItem is PlayerDto;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Add Favorite_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Player" || PublicPlayersList.SelectedItem is not PlayerDto player)
        {
            return;
        }

        var result = await _serviceClient.AddFavoriteAsync(_sessionToken, player.PlayerId);
        ContentMessageText.Text = result.Message;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Remove Favorite_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Player" || FavoritesList.SelectedItem is not PlayerDto player)
        {
            ContentMessageText.Text = "בחר שחקן מהמועדפים להסרה.";
            return;
        }

        var result = await _serviceClient.RemoveFavoriteAsync(_sessionToken, player.PlayerId);
        ContentMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            await RefreshFavoritesAsync();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Refresh Favorites_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void RefreshFavorites_Click(object sender, RoutedEventArgs e) => await RefreshFavoritesAsync();

    /// <summary>מבצעת את שלב המערכת Refresh Favorites Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Remove Favorite_Click, Refresh Favorites_Click.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RefreshFavoritesAsync()
    {
        try
        {
            var result = await _serviceClient.GetMyFavoritesAsync(_sessionToken);
            FavoritesList.ItemsSource = result.Succeeded ? result.Players : null;
            ContentMessageText.Text = result.Message;
        }
        catch (Exception exception)
        {
            ContentMessageText.Text = $"לא הצלחתי לטעון את המועדפים. {exception.Message}";
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Content Tabs_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void ContentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || !ReferenceEquals(e.Source, ContentTabs))
        {
            return;
        }

        if (ReferenceEquals(ContentTabs.SelectedItem, FavoritesTab) && _currentRole == "Player")
        {
            await RefreshFavoritesAsync();
        }
        else if (ReferenceEquals(ContentTabs.SelectedItem, ManagerToolsTab) && _currentRole == "Manager")
        {
            await RefreshManagerContentAsync();
        }
    }

    /// <summary>מבצעת את שלב המערכת Refresh Manager Content Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Content Tabs_Selection Changed, Save Player_Click.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RefreshManagerContentAsync()
    {
        try
        {
            var players = await _serviceClient.GetPlayersAsync();
            var teams = await _serviceClient.GetTeamsAsync();
            ManagerPlayersList.ItemsSource = players.Succeeded ? players.Players : null;
            RosterPlayerPicker.ItemsSource = players.Succeeded ? players.Players : null;
            ManagerTeamsList.ItemsSource = teams.Succeeded ? teams.Teams : null;
            ContentMessageText.Text = players.Succeeded && teams.Succeeded
                ? "נתוני השחקנים והקבוצות נטענו."
                : $"{players.Message} {teams.Message}";
        }
        catch (Exception exception)
        {
            ContentMessageText.Text = $"לא הצלחתי לטעון את נתוני הניהול. {exception.Message}";
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Manager Players List_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ManagerPlayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ManagerPlayersList.SelectedItem is not PlayerDto player)
        {
            return;
        }

        ManagerPlayerFirstName.Text = player.FirstName;
        ManagerPlayerLastName.Text = player.LastName;
        ManagerPlayerAge.Text = player.Age.ToString();
        ManagerPlayerPosition.Text = player.Position;
        ManagerPlayerRating.Text = player.Rating.ToString();
        ManagerPlayerSpeed.Text = player.Speed.ToString();
        ManagerPlayerShooting.Text = player.Shooting.ToString();
        ManagerPlayerPassing.Text = player.Passing.ToString();
        ManagerPlayerDefense.Text = player.Defense.ToString();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Clear Player Form_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך. נקראת בקוד מתוך Save Player_Click, Delete Player_Click.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ClearPlayerForm_Click(object sender, RoutedEventArgs e)
    {
        ManagerPlayersList.SelectedItem = null;
        ManagerPlayerFirstName.Clear();
        ManagerPlayerLastName.Clear();
        ManagerPlayerAge.Clear();
        ManagerPlayerPosition.Clear();
        ManagerPlayerRating.Clear();
        ManagerPlayerSpeed.Clear();
        ManagerPlayerShooting.Clear();
        ManagerPlayerPassing.Clear();
        ManagerPlayerDefense.Clear();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Save Player_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void SavePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ManagerPlayerAge.Text, out var age) ||
            !int.TryParse(ManagerPlayerRating.Text, out var rating) ||
            !int.TryParse(ManagerPlayerSpeed.Text, out var speed) ||
            !int.TryParse(ManagerPlayerShooting.Text, out var shooting) ||
            !int.TryParse(ManagerPlayerPassing.Text, out var passing) ||
            !int.TryParse(ManagerPlayerDefense.Text, out var defense))
        {
            ContentMessageText.Text = "יש להזין מספרים תקינים לגיל ולדירוגים.";
            return;
        }

        var selected = ManagerPlayersList.SelectedItem as PlayerDto;
        var result = await _serviceClient.SavePlayerAsync(_sessionToken, new PlayerDto
        {
            PlayerId = selected?.PlayerId ?? 0,
            FirstName = ManagerPlayerFirstName.Text,
            LastName = ManagerPlayerLastName.Text,
            Age = age,
            Position = ManagerPlayerPosition.Text,
            Rating = rating,
            Speed = speed,
            Shooting = shooting,
            Passing = passing,
            Defense = defense
        });
        ContentMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            ClearPlayerForm_Click(sender, e);
            await RefreshManagerContentAsync();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Delete Player_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void DeletePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (ManagerPlayersList.SelectedItem is not PlayerDto player)
        {
            ContentMessageText.Text = "בחר שחקן למחיקה.";
            return;
        }

        if (MessageBox.Show($"למחוק את {player.FirstName} {player.LastName}?", "מחיקת שחקן", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) != MessageBoxResult.Yes)
        {
            return;
        }

        var result = await _serviceClient.DeletePlayerAsync(_sessionToken, player.PlayerId);
        ContentMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            ClearPlayerForm_Click(sender, e);
            await RefreshManagerContentAsync();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Manager Teams List_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ManagerTeamsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ManagerTeamsList.SelectedItem is not TeamDto team)
        {
            ManagerRosterList.ItemsSource = null;
            return;
        }

        ManagerTeamName.Text = team.Name;
        ManagerTeamCountry.Text = team.Country;
        ManagerTeamLeague.Text = team.League;
        ManagerTeamBudget.Text = team.Budget.ToString("0.##");
        _ = RefreshManagerRosterAsync(team.TeamId);
    }

    /// <summary>מבצעת את שלב המערכת Refresh Manager Roster Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Manager Teams List_Selection Changed, Add Roster Player_Click.</summary>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RefreshManagerRosterAsync(int teamId)
    {
        try
        {
            var result = await _serviceClient.GetTeamRosterAsync(teamId);
            ManagerRosterList.ItemsSource = result.Succeeded ? result.Players : null;
        }
        catch (Exception exception)
        {
            ContentMessageText.Text = $"לא הצלחתי לטעון את הסגל. {exception.Message}";
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Clear Team Form_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך. נקראת בקוד מתוך Save Team_Click, Delete Team_Click.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ClearTeamForm_Click(object sender, RoutedEventArgs e)
    {
        ManagerTeamsList.SelectedItem = null;
        ManagerTeamName.Clear();
        ManagerTeamCountry.Clear();
        ManagerTeamLeague.Clear();
        ManagerTeamBudget.Clear();
        ManagerRosterList.ItemsSource = null;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Save Team_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void SaveTeam_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(ManagerTeamBudget.Text, out var budget))
        {
            ContentMessageText.Text = "יש להזין תקציב מספרי תקין.";
            return;
        }

        var selected = ManagerTeamsList.SelectedItem as TeamDto;
        var result = await _serviceClient.SaveTeamAsync(_sessionToken, new TeamDto
        {
            TeamId = selected?.TeamId ?? 0,
            Name = ManagerTeamName.Text,
            Country = ManagerTeamCountry.Text,
            League = ManagerTeamLeague.Text,
            Budget = budget
        });
        ContentMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            ClearTeamForm_Click(sender, e);
            await RefreshManagerContentAsync();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Delete Team_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void DeleteTeam_Click(object sender, RoutedEventArgs e)
    {
        if (ManagerTeamsList.SelectedItem is not TeamDto team)
        {
            ContentMessageText.Text = "בחר קבוצה למחיקה.";
            return;
        }

        if (MessageBox.Show($"למחוק את {team.Name}?", "מחיקת קבוצה", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading) != MessageBoxResult.Yes)
        {
            return;
        }

        var result = await _serviceClient.DeleteTeamAsync(_sessionToken, team.TeamId);
        ContentMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            ClearTeamForm_Click(sender, e);
            await RefreshManagerContentAsync();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Add Roster Player_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void AddRosterPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (ManagerTeamsList.SelectedItem is not TeamDto team || RosterPlayerPicker.SelectedItem is not PlayerDto player)
        {
            ContentMessageText.Text = "בחר קבוצה ושחקן לשיוך.";
            return;
        }

        var result = await _serviceClient.AddPlayerToTeamAsync(_sessionToken, team.TeamId, player.PlayerId);
        ContentMessageText.Text = result.Message;
        await RefreshManagerRosterAsync(team.TeamId);
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Remove Roster Player_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void RemoveRosterPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (ManagerTeamsList.SelectedItem is not TeamDto team || ManagerRosterList.SelectedItem is not PlayerDto player)
        {
            ContentMessageText.Text = "בחר שחקן מהסגל להסרה.";
            return;
        }

        var result = await _serviceClient.RemovePlayerFromTeamAsync(_sessionToken, team.TeamId, player.PlayerId);
        ContentMessageText.Text = result.Message;
        await RefreshManagerRosterAsync(team.TeamId);
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Show Matches_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ShowMatches_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole is "Player" or "Manager")
            OpenMatches_Click(sender, e);
        else
            ApplyMatchFilter("All");
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Show Lineups_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void ShowLineups_Click(object sender, RoutedEventArgs e)
    {
        if (MatchesListBox.SelectedItem is null && MatchesListBox.Items.Count > 0)
        {
            MatchesListBox.SelectedIndex = 0;
        }

        LineupSection.BringIntoView();
        SelectedHomeTeam.Focus();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Match Filter_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void MatchFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string filter })
        {
            ApplyMatchFilter(filter);
        }
    }

    /// <summary>מבצעת את שלב המערכת Apply Match Filter באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Main Window, Show Matches_Click.</summary>
    /// <param name="filter">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    private void ApplyMatchFilter(string filter)
    {
        SetMatchFilterAppearance(AllMatchesFilter, filter == "All");
        SetMatchFilterAppearance(FinishedMatchesFilter, filter == "Finished");
        SetMatchFilterAppearance(UpcomingMatchesFilter, filter == "Upcoming");

        var filtered = _guestMatches.Filter(filter);

        MatchesListBox.ItemsSource = filtered;
        MatchesListBox.SelectedIndex = filtered.Count == 0 ? -1 : 0;
        NoMatchesText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>מבצעת את פעולת הנתונים Set Match Filter Appearance. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Apply Match Filter.</summary>
    /// <param name="button">ערך מסוג Button שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="selected">דגל המציין אם המצב או הפעולה מופעלים.</param>
    private static void SetMatchFilterAppearance(Button button, bool selected)
    {
        button.Background = selected
            ? new SolidColorBrush(Color.FromRgb(231, 189, 32))
            : new SolidColorBrush(Color.FromRgb(27, 44, 65));
        button.Foreground = selected
            ? new SolidColorBrush(Color.FromRgb(21, 32, 48))
            : new SolidColorBrush(Color.FromRgb(238, 243, 248));
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Matches List Box_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void MatchesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MatchesListBox.SelectedItem is not GuestMatch selected)
        {
            return;
        }

        SelectedHomeTeam.Text = selected.HomeTeam;
        SelectedMatchTitle.Text = $"{selected.HomeTeam} נגד {selected.AwayTeam} · הרכב להמחשה";
    }

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void LoginInput_Changed(object sender, TextChangedEventArgs e)
    {
        _authentication.LoginUsernameTouched = true;
        UpdateLoginValidation();
    }

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void LoginPassword_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncingLoginPassword)
        {
            return;
        }

        _authentication.LoginPasswordTouched = true;
        _syncingLoginPassword = true;
        LoginPasswordTextBox.Text = LoginPasswordBox.Password;
        _syncingLoginPassword = false;
        UpdateLoginValidation();
    }

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void LoginPasswordText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_syncingLoginPassword)
        {
            return;
        }

        _authentication.LoginPasswordTouched = true;
        _syncingLoginPassword = true;
        LoginPasswordBox.Password = LoginPasswordTextBox.Text;
        _syncingLoginPassword = false;
        UpdateLoginValidation();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Login Show Password_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void LoginShowPassword_Click(object sender, RoutedEventArgs e)
    {
        var show = LoginShowPasswordButton.IsChecked == true;
        LoginPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        LoginPasswordTextBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            LoginPasswordTextBox.Focus();
            LoginPasswordTextBox.CaretIndex = LoginPasswordTextBox.Text.Length;
        }
        else
        {
            LoginPasswordBox.Focus();
        }
    }

    /// <summary>מבצעת את שלב המערכת Registration Input_Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegistrationInput_Changed(object sender, TextChangedEventArgs e)
    {
        _authentication.RegisterUsernameTouched = true;
        UpdateRegistrationValidation();
    }

    /// <summary>מבצעת את שלב המערכת Register Password_Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegisterPassword_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncingRegisterPassword)
        {
            return;
        }

        _authentication.RegisterPasswordTouched = true;
        _syncingRegisterPassword = true;
        RegisterPasswordTextBox.Text = RegisterPasswordBox.Password;
        _syncingRegisterPassword = false;
        UpdateRegistrationValidation();
    }

    /// <summary>מבצעת את שלב המערכת Register Password Text_Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegisterPasswordText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_syncingRegisterPassword)
        {
            return;
        }

        _authentication.RegisterPasswordTouched = true;
        _syncingRegisterPassword = true;
        RegisterPasswordBox.Password = RegisterPasswordTextBox.Text;
        _syncingRegisterPassword = false;
        UpdateRegistrationValidation();
    }

    /// <summary>מבצעת את שלב המערכת Register Confirm Password_Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegisterConfirmPassword_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncingRegisterConfirmation)
        {
            return;
        }

        _authentication.RegisterConfirmationTouched = true;
        _syncingRegisterConfirmation = true;
        RegisterConfirmPasswordTextBox.Text = RegisterConfirmPasswordBox.Password;
        _syncingRegisterConfirmation = false;
        UpdateRegistrationValidation();
    }

    /// <summary>מבצעת את שלב המערכת Register Confirm Password Text_Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegisterConfirmPasswordText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_syncingRegisterConfirmation)
        {
            return;
        }

        _authentication.RegisterConfirmationTouched = true;
        _syncingRegisterConfirmation = true;
        RegisterConfirmPasswordBox.Password = RegisterConfirmPasswordTextBox.Text;
        _syncingRegisterConfirmation = false;
        UpdateRegistrationValidation();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Register Show Password_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegisterShowPassword_Click(object sender, RoutedEventArgs e)
    {
        var show = RegisterShowPasswordButton.IsChecked == true;
        RegisterPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        RegisterPasswordTextBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            RegisterPasswordTextBox.Focus();
            RegisterPasswordTextBox.CaretIndex = RegisterPasswordTextBox.Text.Length;
        }
        else
        {
            RegisterPasswordBox.Focus();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Register Show Confirm_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void RegisterShowConfirm_Click(object sender, RoutedEventArgs e)
    {
        var show = RegisterShowConfirmButton.IsChecked == true;
        RegisterConfirmPasswordBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        RegisterConfirmPasswordTextBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            RegisterConfirmPasswordTextBox.Focus();
            RegisterConfirmPasswordTextBox.CaretIndex = RegisterConfirmPasswordTextBox.Text.Length;
        }
        else
        {
            RegisterConfirmPasswordBox.Focus();
        }
    }

    /// <summary>קוראת ל־AuthenticationViewModel לבדוק שם משתמש וסיסמה שהוקלדו, מציגה הודעה ומפעילה או מכבה את כפתור הכניסה. זו בדיקת קלט מקומית בלבד; אימות החשבון נעשה ב־WCF.</summary>
    private void UpdateLoginValidation()
    {
        var validation = _authentication.EvaluateLogin(LoginUsernameTextBox.Text, LoginPassword);
        LoginValidationText.Text = validation.Message;
        LoginButton.IsEnabled = validation.CanSubmit;
    }

    /// <summary>מעבירה שם משתמש, סיסמה ואישור ל־AuthenticationViewModel, מציגה רמזים ליד השדות ומאפשרת שליחה רק כשהנתונים המקומיים תקינים. השירות בודק אותם שוב לפני שמירת חשבון.</summary>
    private void UpdateRegistrationValidation()
    {
        var validation = _authentication.EvaluateRegistration(
            RegisterUsernameTextBox.Text, RegistrationPassword, RegistrationConfirmation);
        SetRegistrationHint(RegisterUsernameHint, validation.UsernameHint);
        SetRegistrationHint(RegisterPasswordHint, validation.PasswordHint);
        SetRegistrationHint(RegisterConfirmPasswordHint, validation.ConfirmationHint);
        RegisterValidationText.Text = validation.Message;
        RegisterValidationText.Foreground = validation.CanSubmit ? Brushes.LightGreen : Brushes.LightGray;
        RegisterButton.IsEnabled = validation.CanSubmit;
    }

    /// <summary>מבצעת את פעולת הנתונים Set Registration Hint. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Update Registration Validation.</summary>
    /// <param name="hint">ערך מסוג TextBlock שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="message">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    private static void SetRegistrationHint(TextBlock hint, string? message)
    {
        hint.Text = message ?? string.Empty;
        hint.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>שולחת שם משתמש וסיסמה ל־WCF. השירות בודק תקינות, מגבב את הסיסמה ושומר את החשבון. התשובה מוצגת למשתמש; הצלחה ממלאת את שם הכניסה ומעבירה ללשונית ההתחברות.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        RegisterButton.IsEnabled = false;
        RegisterMessageText.Text = "שולח הרשמה לשירות...";
        try
        {
            // השמירה והגיבוב מבוצעים בשרת; הלקוח שולח בקשת DTO בלבד.
            var result = await _serviceClient.RegisterAsync(new RegisterRequestDto
            {
                Username = RegisterUsernameTextBox.Text.Trim(),
                Password = RegistrationPassword
            });
            RegisterMessageText.Text = result.Message;
            if (result.Succeeded)
            {
                LoginUsernameTextBox.Text = RegisterUsernameTextBox.Text.Trim();
                AuthTabs.SelectedIndex = 0;
                LoginMessageText.Text = "החשבון נוצר. אפשר להתחבר.";
            }
        }
        catch (Exception exception)
        {
            RegisterMessageText.Text = $"לא הצלחתי להתחבר לשירות. ודא ש־WCF פועל. פרטים: {exception.Message}";
        }
        finally
        {
            UpdateRegistrationValidation();
        }
    }

    /// <summary>שולחת פרטי כניסה ל־WCF, שומרת את Session שחזר בהצלחה ומעדכנת את הדשבורד לפי Role שהשרת החזיר. השירות מאמת PasswordHash; הסתרת כרטיסיות אינה תחליף לבדיקת ההרשאה בשרת. שגיאות מוצגות במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        LoginMessageText.Text = "מתחבר לשירות...";
        try
        {
            // אימות הסיסמה נעשה בשירות מול הגיבוב שנשמר ב־Users.
            var result = await _serviceClient.LoginAsync(new LoginRequestDto
            {
                Username = LoginUsernameTextBox.Text.Trim(),
                Password = LoginPassword
            });

            if (!result.Succeeded)
            {
                LoginMessageText.Text = result.Message;
                return;
            }

            // רכיב Session אחד מעביר את האסימון והמשתמש לקריאות השירות הבאות.
            _session.Start(result);
            WelcomeText.Text = $"{_currentUsername} | תפקיד: {RoleInHebrew(_currentRole)}";
            RoleHomeText.Text = _currentRole switch
            {
                "Admin" => "התחברת כמנהל מערכת. מכאן אפשר לנהל תפקידים וחסימות משתמשים.",
                "Manager" => "התחברת כמאמן. השתמש בניווט כדי לנהל שחקנים, קבוצות וסגלים.",
                _ => "התחברת כשחקן. אפשר לעיין בשחקנים, בקבוצות ולשמור מועדפים."
            };
            AdminPanel.Visibility = _currentRole == "Admin" ? Visibility.Visible : Visibility.Collapsed;
            AdminCardsPanel.Visibility = _currentRole == "Admin" ? Visibility.Visible : Visibility.Collapsed;
            OpenMatchesButton.Visibility = _currentRole is "Player" or "Manager" ? Visibility.Visible : Visibility.Collapsed;
            // זו התאמת תצוגה לנוחות המשתמש; WCF עדיין אוכף את התפקיד בכל מתודת C4.
            OpenCycle4Button.Visibility = _currentRole == "Manager" ? Visibility.Visible : Visibility.Collapsed;
            // כפתור הסקאוט גלוי למשתמשים מחוברים כדי להדגים שגם תפקיד לא מורשה נחסם ב־WCF.
            AiScoutButton.Visibility = Visibility.Visible;
            AiChatButton.Visibility = Visibility.Visible;
            PlayerTransferAttemptButton.Visibility = _currentRole == "Player" && _lastSavedTransfer is not null
                ? Visibility.Visible : Visibility.Collapsed;
            PlayerTransferStatus.Text = string.Empty;
            PlayerTransferStatus.Visibility = Visibility.Collapsed;
            AuthPanel.Visibility = Visibility.Collapsed;
            ContentBrowsePanel.Visibility = Visibility.Collapsed;
            GuestHomePanel.Visibility = Visibility.Collapsed;
            DashboardPanel.Visibility = Visibility.Visible;
            FavoritesTab.Visibility = _currentRole == "Player" ? Visibility.Visible : Visibility.Collapsed;
            ManagerToolsTab.Visibility = _currentRole == "Manager" ? Visibility.Visible : Visibility.Collapsed;
            AddFavoriteButton.Visibility = _currentRole == "Player" ? Visibility.Visible : Visibility.Collapsed;

            if (_currentRole == "Admin")
            {
                await RefreshManagedUsersAsync();
            }
        }
        catch (Exception exception)
        {
            LoginMessageText.Text = $"לא הצלחתי להתחבר לשירות. ודא ש־WCF פועל. פרטים: {exception.Message}";
        }
        finally
        {
            UpdateLoginValidation();
        }
    }

    /// <summary>מבקשת מהשירות לבטל את האסימון, מנקה את Session ואת נתוני המסכים ומחזירה את החלון למצב אורח. אם השירות אינו זמין, מצב הלקוח עדיין מתנקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_sessionToken))
            {
                await _serviceClient.LogoutAsync(_sessionToken);
            }
        }
        catch
        {
            // אם WCF הופעל מחדש, גם ה־Session שבשרת כבר אינו קיים.
        }

        _session.Clear();
        OpenMatchesButton.Visibility = Visibility.Collapsed;
        OpenCycle4Button.Visibility = Visibility.Collapsed;
        AiScoutButton.Visibility = Visibility.Collapsed;
        AiChatButton.Visibility = Visibility.Collapsed;
        PlayerTransferAttemptButton.Visibility = Visibility.Collapsed;
        PlayerTransferStatus.Visibility = Visibility.Collapsed;
        _cachedManagedUsers = [];
        AdminUsersList.ItemsSource = null;
        AdminPanel.Visibility = Visibility.Collapsed;
        AdminCardsPanel.Visibility = Visibility.Collapsed;
        DashboardPanel.Visibility = Visibility.Collapsed;
        AuthPanel.Visibility = Visibility.Collapsed;
        ContentBrowsePanel.Visibility = Visibility.Collapsed;
        GuestHomePanel.Visibility = Visibility.Visible;
        FavoritesTab.Visibility = Visibility.Collapsed;
        ManagerToolsTab.Visibility = Visibility.Collapsed;
        AddFavoriteButton.Visibility = Visibility.Collapsed;
        LoginMessageText.Text = string.Empty;
        LoginPasswordBox.Clear();
        LoginPasswordTextBox.Clear();
        LoginShowPasswordButton.IsChecked = false;
        LoginPasswordBox.Visibility = Visibility.Visible;
        LoginPasswordTextBox.Visibility = Visibility.Collapsed;
        _authentication.LoginPasswordTouched = false;
        UpdateLoginValidation();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Refresh Users_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void RefreshUsers_Click(object sender, RoutedEventArgs e) => await RefreshManagedUsersAsync();

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Card Users_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void AdminCardUsers_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "ניהול משתמשים";
        await RefreshManagedUsersAsync();
        AdminMessageText.Text = "ניהול חשבונות פעיל: יצירה, שינוי תפקיד, חסימה ומחיקה.";
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Card Control_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void AdminCardControl_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "בקרה על חשבונות";
        await RefreshManagedUsersAsync();
        AdminUsersList.ItemsSource = _cachedManagedUsers.Where(user => user.IsBlocked).ToList();
        AdminMessageText.Text = $"חשבונות חסומים: {_cachedManagedUsers.Count(user => user.IsBlocked)}. בחר חשבון כדי לבטל חסימה.";
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Card Stats_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void AdminCardStats_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "סטטיסטיקת חשבונות";
        await RefreshManagedUsersAsync();
        AdminUsersList.ItemsSource = _cachedManagedUsers;
        var active = _cachedManagedUsers.Count(user => !user.IsBlocked);
        var players = _cachedManagedUsers.Count(user => user.Role == "Player");
        var managers = _cachedManagedUsers.Count(user => user.Role == "Manager");
        var admins = _cachedManagedUsers.Count(user => user.Role == "Admin");
        AdminMessageText.Text = $"סה״כ {_cachedManagedUsers.Count} חשבונות · פעילים {active} · שחקנים {players} · מאמנים {managers} · מנהלי מערכת {admins}.";
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Card Communication_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void AdminCardCommunication_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "ספר משתמשים · תקשורת";
        await RefreshManagedUsersAsync();
        AdminUsersList.ItemsSource = _cachedManagedUsers;
        AdminMessageText.Text = "רשימת החשבונות נטענת מטבלת Users. במסד אין כתובות דוא״ל או הודעות, לכן לא מוצגת שליחת הודעות מדומה.";
    }

    /// <summary>מבצעת את שלב המערכת Refresh Managed Users Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Login_Click, Refresh Users_Click.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private async Task RefreshManagedUsersAsync()
    {
        try
        {
            var result = await _serviceClient.GetManagedUsersAsync(_sessionToken);
            AdminMessageText.Text = result.Message;
            _cachedManagedUsers = result.Succeeded ? result.Users : [];
            AdminUsersList.ItemsSource = result.Succeeded ? _cachedManagedUsers : null;
            AdminUsersCardValue.Text = $"{_cachedManagedUsers.Count} חשבונות";
            AdminControlCardValue.Text = $"{_cachedManagedUsers.Count(user => user.IsBlocked)} חסומים";
            AdminStatsCardValue.Text = $"{_cachedManagedUsers.Count(user => !user.IsBlocked)} פעילים";
            AdminCommunicationCardValue.Text = $"ספר משתמשים · {_cachedManagedUsers.Count}";
        }
        catch (Exception exception)
        {
            AdminMessageText.Text = $"לא הצלחתי לטעון משתמשים: {exception.Message}";
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Users List_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void AdminUsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AdminUsersList.SelectedItem is not ManagedUserDto selected)
        {
            ChangeRoleButton.IsEnabled = false;
            BlockUserButton.IsEnabled = false;
            DeleteManagedUserButton.IsEnabled = false;
            return;
        }

        AdminRoleComboBox.SelectedItem = AdminRoleComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), selected.Role, StringComparison.Ordinal));
        BlockUserButton.Content = selected.IsBlocked ? "בטל חסימה" : "חסום משתמש";
        ChangeRoleButton.IsEnabled = true;
        BlockUserButton.IsEnabled = true;
        DeleteManagedUserButton.IsEnabled = true;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Role Combo Box_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void AdminRoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChangeRoleButton is null || AdminUsersList is null || sender is not ComboBox roleComboBox)
        {
            return;
        }

        ChangeRoleButton.IsEnabled =
            AdminUsersList.SelectedItem is ManagedUserDto &&
            roleComboBox.SelectedItem is ComboBoxItem;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Change Role_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void ChangeRole_Click(object sender, RoutedEventArgs e)
    {
        if (AdminUsersList.SelectedItem is not ManagedUserDto selected ||
            AdminRoleComboBox.SelectedItem is not ComboBoxItem roleItem)
        {
            return;
        }

        var result = await _serviceClient.UpdateUserRoleAsync(
            _sessionToken,
            selected.UserId,
            roleItem.Tag?.ToString() ?? string.Empty);
        AdminMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            await RefreshManagedUsersAsync();
        }
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Block User_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void BlockUser_Click(object sender, RoutedEventArgs e)
    {
        if (AdminUsersList.SelectedItem is not ManagedUserDto selected)
        {
            return;
        }

        var result = await _serviceClient.SetUserBlockedAsync(
            _sessionToken,
            selected.UserId,
            !selected.IsBlocked);
        AdminMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            await RefreshManagedUsersAsync();
        }
    }

    /// <summary>מבצעת את שלב המערכת Admin Create Input_Changed באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void AdminCreateInput_Changed(object sender, RoutedEventArgs e) => UpdateAdminCreateValidation();

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Admin Create Role_Selection Changed ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private void AdminCreateRole_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateAdminCreateValidation();

    /// <summary>מבצעת את פעולת הנתונים Update Admin Create Validation. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Admin Create Input_Changed, Admin Create Role_Selection Changed.</summary>
    private void UpdateAdminCreateValidation()
    {
        if (CreateManagedUserButton is null || AdminNewRoleComboBox is null)
        {
            return;
        }

        var roleSelected = AdminNewRoleComboBox.SelectedItem is ComboBoxItem item &&
            item.Tag?.ToString() is "Player" or "Manager" or "Admin";
        CreateManagedUserButton.IsEnabled =
            AuthenticationViewModel.ValidateUsername(AdminNewUsernameTextBox.Text) is null &&
            AuthenticationViewModel.ValidatePassword(AdminNewPasswordBox.Password) is null &&
            roleSelected;
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Create Managed User_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void CreateManagedUser_Click(object sender, RoutedEventArgs e)
    {
        UpdateAdminCreateValidation();
        if (!CreateManagedUserButton.IsEnabled || AdminNewRoleComboBox.SelectedItem is not ComboBoxItem roleItem)
        {
            AdminMessageText.Text = "בדוק את שם המשתמש והסיסמה.";
            return;
        }

        CreateManagedUserButton.IsEnabled = false;
        var result = await _serviceClient.CreateManagedUserAsync(_sessionToken, new CreateManagedUserRequestDto
        {
            Username = AdminNewUsernameTextBox.Text.Trim(),
            Password = AdminNewPasswordBox.Password,
            Role = roleItem.Tag?.ToString() ?? string.Empty
        });
        AdminMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            AdminNewPasswordBox.Clear();
            await RefreshManagedUsersAsync();
        }

        UpdateAdminCreateValidation();
    }

    /// <summary>מופעלת בעקבות אירוע ממשק המשתמש Delete Managed User_Click ומחברת בין פעולת המשתמש לבין הטיפול במסך.</summary>
    /// <param name="sender">ערך מסוג object שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="e">פרטי האירוע ש-WPF מוסר בעת הפעלת הפעולה.</param>
    private async void DeleteManagedUser_Click(object sender, RoutedEventArgs e)
    {
        if (AdminUsersList.SelectedItem is not ManagedUserDto selected)
        {
            return;
        }

        var answer = MessageBox.Show(
            $"למחוק את החשבון של {selected.Username}?",
            "אישור מחיקת חשבון",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No,
            MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        var result = await _serviceClient.DeleteManagedUserAsync(_sessionToken, selected.UserId);
        AdminMessageText.Text = result.Message;
        if (result.Succeeded)
        {
            await RefreshManagedUsersAsync();
        }
    }

    /// <summary>מבצעת את שלב המערכת Role In Hebrew באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Login_Click.</summary>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>מחרוזת שמייצגת את תוצאת הפעולה.</returns>
    private static string RoleInHebrew(string role) => role switch
    {
        "Admin" => "מנהל מערכת",
        "Manager" => "מאמן",
        _ => "שחקן"
    };
}
