// מטרה: מציג את תוכן סבבים 1 עד 3 בלקוח Android.
// מי משתמש: כפתורי התפריט והכרטיסים של MainPage.
// פעולות: הצגת שחקנים, קבוצות, סגלים, משחקים, מועדפים וניהול משתמשים.
// תלויות: DTO מהספרייה המשותפת ופעולות WCF דרך WcfClient.
using FootballManager.Contracts;

namespace FootballManager.AndroidClient.Views;

public sealed partial class MainPage
{
    /// <summary>שולף קטלוג ציבורי או מועדפים של האסימון. מועדפים מוגבלים ל־Player, עם 401 או 403 מהשרת במקרה דחייה.</summary>
    /// <param name="favorites">רשימת המועדפים של החשבון, או דגל מצב במסך השחקנים.</param>
    private async Task ShowPlayersAsync(bool favorites)
    {
        StartScreen(favorites ? "המועדפים שלי" : "שחקנים");
        var result = favorites
            ? await _service.CallAsync<PlayersResponseDto>("GetMyFavorites", ("sessionToken", _token))
            : await GetPlayersAsync();
        if (!result.Succeeded) { SetNotice(result.Message, true); return; }
        if (_role == "Manager" && !favorites)
            _screen.Children.Add(Button("שחקן חדש", () => EditPlayer(new()), primary: true));
        if (result.Players.Count == 0) SetNotice("אין שחקנים להצגה.");
        foreach (var player in result.Players)
        {
            var card = Card($"{player.FirstName} {player.LastName}", $"{player.Position} · דירוג {player.Rating}");
            if (_role == "Player")
                card.Children.Add(AsyncButton(favorites ? "הסר מהמועדפים" : "הוסף למועדפים", async () =>
                {
                    await ChangeAsync(favorites ? "RemoveFavorite" : "AddFavorite", () => ShowPlayersAsync(favorites),
                        ("playerId", player.PlayerId));
                }));
            if (_role == "Manager" && !favorites)
            {
                card.Children.Add(Button("ערוך שחקן", () => EditPlayer(player)));
                card.Children.Add(AsyncButton("מחק שחקן", () => DeleteAsync("DeletePlayer", "playerId", player.PlayerId,
                    () => ShowPlayersAsync(false))));
            }
        }
    }

    /// <summary>מציג טופס שחקן. שמירה דורשת Manager ונחסמת בשרת ב־401 או 403 אם ההרשאה חסרה.</summary>
    /// <param name="player">רשומת השחקן שנבחרה או נערכת.</param>
    private void EditPlayer(PlayerDto player)
    {
        StartScreen(player.PlayerId == 0 ? "שחקן חדש" : "עריכת שחקן");
        var first = Field("שם פרטי", player.FirstName);
        var last = Field("שם משפחה", player.LastName);
        var position = Field("עמדה", player.Position);
        var values = new[] { ("גיל", player.Age), ("דירוג", player.Rating), ("מהירות", player.Speed),
            ("בעיטה", player.Shooting), ("מסירה", player.Passing), ("הגנה", player.Defense) };
        var entries = values.Select(field => Field(field.Item1, field.Item2.ToString(), number: true)).ToArray();
        _screen.Children.Add(AsyncButton("שמור שחקן", async () =>
        {
            var data = new int[entries.Length];
            for (var i = 0; i < entries.Length; i++)
            {
                if (!int.TryParse(entries[i].Text, out data[i]))
                {
                    SetNotice("יש למלא גיל ודירוגים במספרים שלמים.", true);
                    return;
                }
            }
            var updated = new PlayerDto { PlayerId = player.PlayerId, FirstName = first.Text ?? "", LastName = last.Text ?? "",
                Position = position.Text ?? "", Age = data[0], Rating = data[1], Speed = data[2], Shooting = data[3],
                Passing = data[4], Defense = data[5] };
            await ChangeAsync("SavePlayer", () => ShowPlayersAsync(false), ("player", updated));
        }, primary: true));
    }

    /// <summary>טוען קבוצות ציבוריות ומציג פעולות עריכה רק בתפריט מאמן.</summary>
    private async Task ShowTeamsAsync()
    {
        StartScreen("קבוצות וסגלים");
        var result = await _service.CallAsync<TeamsResponseDto>("GetTeams");
        if (!result.Succeeded) { SetNotice(result.Message, true); return; }
        if (_role == "Manager") _screen.Children.Add(Button("קבוצה חדשה", () => EditTeam(new()), primary: true));
        if (result.Teams.Count == 0) SetNotice("לא נמצאו קבוצות.");
        foreach (var team in result.Teams)
        {
            var card = Card(team.Name, $"{team.Country} · {team.League}");
            card.Children.Add(AsyncButton("פתח סגל", () => ShowRosterAsync(team)));
            if (_role != "Manager") continue;
            card.Children.Add(Button("ערוך קבוצה", () => EditTeam(team)));
            card.Children.Add(AsyncButton("מחק קבוצה", () => DeleteAsync("DeleteTeam", "teamId", team.TeamId, ShowTeamsAsync)));
        }
    }

    /// <summary>מציג טופס קבוצה. מעביר את התקציב ואת שאר השדות לשירות שמאמת את המאמן והנתונים.</summary>
    /// <param name="team">רשומת הקבוצה שבחר המשתמש.</param>
    private void EditTeam(TeamDto team)
    {
        StartScreen(team.TeamId == 0 ? "קבוצה חדשה" : "עריכת קבוצה");
        var name = Field("שם קבוצה", team.Name);
        var country = Field("מדינה", team.Country);
        var league = Field("ליגה", team.League);
        var budget = Field("תקציב", team.Budget.ToString(), number: true);
        _screen.Children.Add(AsyncButton("שמור קבוצה", async () =>
        {
            if (!decimal.TryParse(budget.Text, out var amount)) { SetNotice("תקציב צריך להיות מספר.", true); return; }
            await ChangeAsync("SaveTeam", ShowTeamsAsync, ("team", new TeamDto
                { TeamId = team.TeamId, Name = name.Text ?? "", Country = country.Text ?? "", League = league.Text ?? "", Budget = amount }));
        }, primary: true));
    }

    /// <summary>מציג את סגל הקבוצה וטוען למאמן את השחקנים הזמינים לשיוך. השרת אוכף 401 ו־403 בשינוי הסגל.</summary>
    /// <param name="team">רשומת הקבוצה שבחר המשתמש.</param>
    private async Task ShowRosterAsync(TeamDto team)
    {
        StartScreen("סגל " + team.Name);
        var result = await _service.CallAsync<PlayersResponseDto>("GetTeamRoster", ("teamId", team.TeamId));
        if (!result.Succeeded) { SetNotice(result.Message, true); return; }
        if (_role == "Manager")
        {
            var all = await GetPlayersAsync();
            var assigned = result.Players.Select(p => p.PlayerId).ToHashSet();
            var available = all.Players.Where(p => !assigned.Contains(p.PlayerId)).ToList();
            if (available.Count > 0)
            {
                var pick = Choice("בחר שחקן לשיוך", available);
                _screen.Children.Add(AsyncButton("הוסף לסגל", async () =>
                {
                    if (pick.SelectedItem is PlayerDto player)
                        await ChangeAsync("AddPlayerToTeam", () => ShowRosterAsync(team),
                            ("teamId", team.TeamId), ("playerId", player.PlayerId));
                    else SetNotice("בחר שחקן לפני השיוך.", true);
                }, primary: true));
            }
        }
        foreach (var player in result.Players)
        {
            var card = Card($"{player.FirstName} {player.LastName}", player.Position);
            if (_role == "Manager")
                card.Children.Add(AsyncButton("הסר מהסגל", () => ChangeAsync("RemovePlayerFromTeam", () => ShowRosterAsync(team),
                    ("teamId", team.TeamId), ("playerId", player.PlayerId))));
        }
        if (result.Players.Count == 0) _screen.Children.Add(Text("אין שחקנים בסגל.", 15));
    }

    /// <summary>קורא משחקים מ־WCF. אורח מקבל 401, אדמין מקבל 403, שחקן קורא ומאמן יכול לערוך.</summary>
    private async Task ShowMatchesAsync()
    {
        StartScreen("משחקים ותוצאות");
        var result = await _service.CallAsync<MatchesResponseDto>("GetMatches",
            ("sessionToken", _token), ("searchText", ""), ("matchDate", null));
        if (!result.Succeeded) { SetNotice(result.Message, true); return; }
        if (_role == "Manager") _screen.Children.Add(AsyncButton("משחק חדש", () => EditMatchAsync(new()), primary: true));
        if (result.Matches.Count == 0) SetNotice("לא נמצאו משחקים.");
        foreach (var match in result.Matches)
        {
            var card = Card($"{match.HomeTeamName} מול {match.AwayTeamName}",
                $"{match.MatchDate:dd/MM/yyyy} · {match.HomeScore} : {match.AwayScore}");
            if (_role != "Manager") continue;
            card.Children.Add(AsyncButton("עדכן משחק", () => EditMatchAsync(match)));
            card.Children.Add(AsyncButton("מחק משחק", () => DeleteAsync("DeleteMatch", "matchId", match.MatchId, ShowMatchesAsync)));
        }
    }

    /// <summary>טוען קבוצות לעריכת משחק ושולח תאריך ותוצאה ל־SaveMatch. השרת מאפשר שמירה למאמן בלבד.</summary>
    /// <param name="match">נתוני משחק, כולל תאריך, מזהי הקבוצות ושערים.</param>
    private async Task EditMatchAsync(MatchDto match)
    {
        StartScreen(match.MatchId == 0 ? "משחק חדש" : "עריכת משחק");
        var response = await _service.CallAsync<TeamsResponseDto>("GetTeams");
        if (!response.Succeeded) { SetNotice(response.Message, true); return; }
        var teams = response.Teams;
        var home = Choice("קבוצת בית", teams, teams.FindIndex(t => t.TeamId == match.HomeTeamId));
        var away = Choice("קבוצת חוץ", teams, teams.FindIndex(t => t.TeamId == match.AwayTeamId));
        _screen.Children.Add(Text("תאריך המשחק", 14));
        var date = new DatePicker { Date = match.MatchId == 0 ? DateTime.Today : match.MatchDate,
            TextColor = Light, BackgroundColor = Panel };
        _screen.Children.Add(date);
        var homeScore = Field("שערי בית", match.HomeScore.ToString(), number: true);
        var awayScore = Field("שערי חוץ", match.AwayScore.ToString(), number: true);
        _screen.Children.Add(AsyncButton("שמור משחק", async () =>
        {
            if (home.SelectedItem is not TeamDto h || away.SelectedItem is not TeamDto a || h.TeamId == a.TeamId ||
                !int.TryParse(homeScore.Text, out var x) || x < 0 || !int.TryParse(awayScore.Text, out var y) || y < 0)
            {
                SetNotice("בחר שתי קבוצות שונות והזן תוצאה תקינה.", true);
                return;
            }
            await ChangeAsync("SaveMatch", ShowMatchesAsync, ("match", new MatchDto { MatchId = match.MatchId,
                HomeTeamId = h.TeamId, AwayTeamId = a.TeamId, MatchDate = date.Date, HomeScore = x, AwayScore = y }));
        }, primary: true));
    }

    /// <summary>טוען את רשימת החשבונות לאדמין. השרת מחזיר 401 בלי התחברות ו־403 לתפקיד אחר.</summary>
    private async Task ShowUsersAsync()
    {
        StartScreen("ניהול משתמשים");
        var result = await _service.CallAsync<ManagedUsersResponseDto>("GetManagedUsers", ("sessionToken", _token));
        if (!result.Succeeded) { SetNotice(result.Message, true); return; }
        _screen.Children.Add(Button("יצירת משתמש", () => EditUser(null), primary: true));
        foreach (var user in result.Users)
        {
            var card = Card(user.Username, $"{RoleLabel(user.Role)} · {(user.IsBlocked ? "חסום" : "פעיל")}");
            card.Children.Add(Button("שנה תפקיד", () => EditUser(user)));
            card.Children.Add(AsyncButton(user.IsBlocked ? "בטל חסימה" : "חסום", () => ChangeAsync("SetUserBlocked",
                ShowUsersAsync, ("userId", user.UserId), ("isBlocked", !user.IsBlocked))));
            card.Children.Add(AsyncButton("מחק חשבון", () => DeleteAsync("DeleteManagedUser", "userId", user.UserId, ShowUsersAsync)));
        }
    }

    /// <summary>מציג בחירת תפקיד לאדמין, או טופס יצירת חשבון. השרת בודק הרשאות ומגבלות על האדמין האחרון.</summary>
    /// <param name="user">רשומת החשבון הנבחר, או null כשפותחים חשבון.</param>
    private void EditUser(ManagedUserDto? user)
    {
        StartScreen(user is null ? "יצירת משתמש" : $"תפקיד של {user.Username}");
        var name = user is null ? Field("שם משתמש") : null;
        var password = user is null ? Field("סיסמה", secret: true) : null;
        var roles = new[] { "Player", "Manager", "Admin" };
        var role = Choice("תפקיד", roles, user is null ? 0 : Array.IndexOf(roles, user.Role));
        _screen.Children.Add(AsyncButton("שמור", async () =>
        {
            if (role.SelectedItem is not string selected) { SetNotice("יש לבחור תפקיד.", true); return; }
            if (user is not null)
            {
                await ChangeAsync("UpdateUserRole", ShowUsersAsync, ("userId", user.UserId), ("role", selected));
                return;
            }
            var username = name?.Text?.Trim() ?? "";
            var pass = password?.Text ?? "";
            if (!ValidUsername(username) || pass.Length is < 8 or > 128)
            { SetNotice("יש להזין שם משתמש תקין וסיסמה בת 8 עד 128 תווים.", true); return; }
            await ChangeAsync("CreateManagedUser", ShowUsersAsync, ("request", new CreateManagedUserRequestDto
                { Username = username, Password = pass, Role = selected }));
        }, primary: true));
    }

    /// <summary>מבקש אישור לפני מחיקה ומעביר לשירות את מזהה הרשומה.</summary>
    /// <param name="operation">שם המתודה לפי חוזה WCF.</param>
    /// <param name="key">שם הפרמטר עבור מזהה הרשומה.</param>
    /// <param name="id">מזהה הרשומה למחיקה.</param>
    /// <param name="reload">פעולה שקוראת מחדש את הרשומות לאחר שינוי מוצלח.</param>
    private async Task DeleteAsync(string operation, string key, int id, Func<Task> reload)
    {
        if (await DisplayAlert("מחיקת רשומה", "למחוק את הרשומה מהמערכת?", "מחק", "ביטול"))
            await ChangeAsync(operation, reload, (key, id));
    }

    /// <summary>מוסיף אסימון לכל כתיבה, ומרענן נתונים מהשרת רק אחרי שהשמירה הצליחה.</summary>
    /// <param name="operation">שם המתודה לפי חוזה WCF.</param>
    /// <param name="reload">פעולה שקוראת מחדש את הרשומות לאחר שינוי מוצלח.</param>
    /// <param name="fields">פרמטרים נוספים שנשלחים לצד האסימון.</param>
    private async Task ChangeAsync(string operation, Func<Task> reload, params (string Name, object? Value)[] fields)
    {
        var arguments = new (string Name, object? Value)[] { ("sessionToken", _token) }.Concat(fields).ToArray();
        var result = await _service.CallAsync<OperationResultDto>(operation, arguments);
        if (result.Succeeded) await reload();
        SetNotice(result.Message, !result.Succeeded);
    }

    /// <summary>טוען את כל השחקנים הציבוריים מטווח הדירוג של החוזה.</summary>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    private Task<PlayersResponseDto> GetPlayersAsync() => _service.CallAsync<PlayersResponseDto>("GetPlayers",
        ("searchText", ""), ("position", ""), ("minimumRating", 0), ("maximumRating", 100));

    /// <summary>מוסיף כרטיס עם שורת תיאור ומקום לפעולות.</summary>
    /// <param name="title">טקסט הכותרת או הכפתור.</param>
    /// <param name="details">שורת הפרטים מתחת לשם הרשומה.</param>
    private VerticalStackLayout Card(string title, string details)
    {
        var headline = Text(title, 18, Amber);
        headline.FontAttributes = FontAttributes.Bold;
        var content = new VerticalStackLayout { Spacing = 8, Children = { headline, Text(details, 15) } };
        _screen.Children.Add(new Border { BackgroundColor = Panel, StrokeThickness = 0,
            Padding = new Thickness(13, 10), Content = content });
        return content;
    }
}
