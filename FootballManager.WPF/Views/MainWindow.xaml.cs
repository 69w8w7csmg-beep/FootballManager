// טיפול בתצוגה ובאירועי החלון הראשי. בדיקות טפסי החשבון נמצאות ב־ViewModel.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FootballManager.Contracts;
using FootballManager.WPF.Models;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

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

    private void OpenLogin_Click(object sender, RoutedEventArgs e) => OpenAuthentication(0);

    private void OpenMatches_Click(object sender, RoutedEventArgs e) =>
        new MatchesWindow(_serviceClient, _session) { Owner = this }.ShowDialog();

    private void OpenCycle4_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Manager") return;
        new Cycle4Window(_serviceClient, _session, RememberSavedTransfer) { Owner = this }.ShowDialog();
    }

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

    private async void PlayerTransferAttempt_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Player" || _lastSavedTransfer is null) return;
        PlayerTransferAttemptButton.IsEnabled = false;
        try
        {
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

    private void OpenPlayerRegistration_Click(object sender, RoutedEventArgs e) => OpenRegistration();

    private void OpenRegistration()
    {
        OpenAuthentication(1);
        UpdateRegistrationValidation();
    }

    private void OpenAuthentication(int tabIndex)
    {
        GuestHomePanel.Visibility = Visibility.Collapsed;
        ContentBrowsePanel.Visibility = Visibility.Collapsed;
        DashboardPanel.Visibility = Visibility.Collapsed;
        AuthPanel.Visibility = Visibility.Visible;
        AuthTabs.SelectedIndex = tabIndex;
    }

    private void ShowGuest_Click(object sender, RoutedEventArgs e)
    {
        AuthPanel.Visibility = Visibility.Collapsed;
        ContentBrowsePanel.Visibility = Visibility.Collapsed;
        var isLoggedIn = !string.IsNullOrEmpty(_sessionToken);
        DashboardPanel.Visibility = isLoggedIn ? Visibility.Visible : Visibility.Collapsed;
        GuestHomePanel.Visibility = isLoggedIn ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void ShowPlayers_Click(object sender, RoutedEventArgs e)
    {
        OpenContentBrowser(0);
        await RefreshPublicPlayersAsync();
    }

    private async void ShowTeams_Click(object sender, RoutedEventArgs e)
    {
        OpenContentBrowser(1);
        await RefreshPublicTeamsAsync();
    }

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

    private async void SearchPlayers_Click(object sender, RoutedEventArgs e) => await RefreshPublicPlayersAsync();

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

    private void PublicPlayersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AddFavoriteButton.IsEnabled = _currentRole == "Player" && PublicPlayersList.SelectedItem is PlayerDto;
    }

    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole != "Player" || PublicPlayersList.SelectedItem is not PlayerDto player)
        {
            return;
        }

        var result = await _serviceClient.AddFavoriteAsync(_sessionToken, player.PlayerId);
        ContentMessageText.Text = result.Message;
    }

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

    private async void RefreshFavorites_Click(object sender, RoutedEventArgs e) => await RefreshFavoritesAsync();

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

    private void ClearTeamForm_Click(object sender, RoutedEventArgs e)
    {
        ManagerTeamsList.SelectedItem = null;
        ManagerTeamName.Clear();
        ManagerTeamCountry.Clear();
        ManagerTeamLeague.Clear();
        ManagerTeamBudget.Clear();
        ManagerRosterList.ItemsSource = null;
    }

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

    private void ShowMatches_Click(object sender, RoutedEventArgs e)
    {
        if (_currentRole is "Player" or "Manager")
            OpenMatches_Click(sender, e);
        else
            ApplyMatchFilter("All");
    }

    private void ShowLineups_Click(object sender, RoutedEventArgs e)
    {
        if (MatchesListBox.SelectedItem is null && MatchesListBox.Items.Count > 0)
        {
            MatchesListBox.SelectedIndex = 0;
        }

        LineupSection.BringIntoView();
        SelectedHomeTeam.Focus();
    }

    private void MatchFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string filter })
        {
            ApplyMatchFilter(filter);
        }
    }

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

    private static void SetMatchFilterAppearance(Button button, bool selected)
    {
        button.Background = selected
            ? new SolidColorBrush(Color.FromRgb(231, 189, 32))
            : new SolidColorBrush(Color.FromRgb(27, 44, 65));
        button.Foreground = selected
            ? new SolidColorBrush(Color.FromRgb(21, 32, 48))
            : new SolidColorBrush(Color.FromRgb(238, 243, 248));
    }

    private void MatchesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MatchesListBox.SelectedItem is not GuestMatch selected)
        {
            return;
        }

        SelectedHomeTeam.Text = selected.HomeTeam;
        SelectedMatchTitle.Text = $"{selected.HomeTeam} נגד {selected.AwayTeam} · הרכב להמחשה";
    }

    private void LoginInput_Changed(object sender, TextChangedEventArgs e)
    {
        _authentication.LoginUsernameTouched = true;
        UpdateLoginValidation();
    }

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

    private void RegistrationInput_Changed(object sender, TextChangedEventArgs e)
    {
        _authentication.RegisterUsernameTouched = true;
        UpdateRegistrationValidation();
    }

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

    private void UpdateLoginValidation()
    {
        var validation = _authentication.EvaluateLogin(LoginUsernameTextBox.Text, LoginPassword);
        LoginValidationText.Text = validation.Message;
        LoginButton.IsEnabled = validation.CanSubmit;
    }

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

    private static void SetRegistrationHint(TextBlock hint, string? message)
    {
        hint.Text = message ?? string.Empty;
        hint.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        RegisterButton.IsEnabled = false;
        RegisterMessageText.Text = "שולח הרשמה לשירות...";
        try
        {
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

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginButton.IsEnabled = false;
        LoginMessageText.Text = "מתחבר לשירות...";
        try
        {
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
            OpenCycle4Button.Visibility = _currentRole == "Manager" ? Visibility.Visible : Visibility.Collapsed;
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
            // A local WCF restart also ends the server-side session.
        }

        _session.Clear();
        OpenMatchesButton.Visibility = Visibility.Collapsed;
        OpenCycle4Button.Visibility = Visibility.Collapsed;
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

    private async void RefreshUsers_Click(object sender, RoutedEventArgs e) => await RefreshManagedUsersAsync();

    private async void AdminCardUsers_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "ניהול משתמשים";
        await RefreshManagedUsersAsync();
        AdminMessageText.Text = "ניהול חשבונות פעיל: יצירה, שינוי תפקיד, חסימה ומחיקה.";
    }

    private async void AdminCardControl_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "בקרה על חשבונות";
        await RefreshManagedUsersAsync();
        AdminUsersList.ItemsSource = _cachedManagedUsers.Where(user => user.IsBlocked).ToList();
        AdminMessageText.Text = $"חשבונות חסומים: {_cachedManagedUsers.Count(user => user.IsBlocked)}. בחר חשבון כדי לבטל חסימה.";
    }

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

    private async void AdminCardCommunication_Click(object sender, RoutedEventArgs e)
    {
        AdminPanel.Header = "ספר משתמשים · תקשורת";
        await RefreshManagedUsersAsync();
        AdminUsersList.ItemsSource = _cachedManagedUsers;
        AdminMessageText.Text = "רשימת החשבונות נטענת מטבלת Users. במסד אין כתובות דוא״ל או הודעות, לכן לא מוצגת שליחת הודעות מדומה.";
    }

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

    private void AdminCreateInput_Changed(object sender, RoutedEventArgs e) => UpdateAdminCreateValidation();

    private void AdminCreateRole_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateAdminCreateValidation();

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

    private static string RoleInHebrew(string role) => role switch
    {
        "Admin" => "מנהל מערכת",
        "Manager" => "מאמן",
        _ => "שחקן"
    };
}
