using CoreWCF;
using Microsoft.AspNetCore.Identity;
using FootballManager.Contracts;
using FootballManager.WCF.Contracts;
using FootballManager.WCF.Data;

namespace FootballManager.WCF.Services;

[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
public sealed partial class FootballManagerService : IFootballManagerService
{
    private readonly AccessDatabase _database;
    private readonly AccessUserRepository _users;
    private readonly AccessFootballRepository _football;
    private readonly AccessMatchRepository _matches;
    private readonly AccessTransferRepository _transfers;
    private readonly AccessPlayerStatisticRepository _statistics;
    private readonly PasswordHasher<UserRecord> _passwordHasher;
    private readonly LoginSessionStore _sessions;

    public FootballManagerService(
        AccessDatabase database,
        AccessUserRepository users,
        AccessFootballRepository football,
        AccessMatchRepository matches,
        AccessTransferRepository transfers,
        AccessPlayerStatisticRepository statistics,
        PasswordHasher<UserRecord> passwordHasher,
        LoginSessionStore sessions)
    {
        _database = database;
        _users = users;
        _football = football;
        _matches = matches;
        _transfers = transfers;
        _statistics = statistics;
        _passwordHasher = passwordHasher;
        _sessions = sessions;
    }

    public ServiceStatusDto GetServiceStatus()
    {
        return new ServiceStatusDto
        {
            ApplicationName = "FootballManager WCF",
            DatabaseReady = _database.IsReady,
            Message = _database.IsReady
                ? "שירות WCF ומסד Access מוכנים."
                : "מסד Access עדיין לא מוכן."
        };
    }

    public OperationResultDto Register(RegisterRequestDto request)
    {
        var username = request?.Username?.Trim() ?? string.Empty;
        var usernameError = AccountRules.ValidateUsername(username);
        if (usernameError is not null)
        {
            return Failure(usernameError);
        }

        var passwordError = AccountRules.ValidatePassword(request?.Password);
        if (passwordError is not null)
        {
            return Failure(passwordError);
        }

        var account = new UserRecord { Username = username, Role = "Player" };
        var passwordHash = _passwordHasher.HashPassword(account, request!.Password);
        try
        {
            return _users.TryCreatePublicAccount(username, passwordHash)
                ? Success("חשבון שחקן נוצר. אפשר להתחבר.")
                : Failure("שם המשתמש כבר קיים.");
        }
        catch
        {
            return Failure("לא ניתן ליצור את החשבון כרגע. בדוק את מסד הנתונים ונסה שוב.");
        }
    }

    public LoginResponseDto Login(LoginRequestDto request)
    {
        var username = request?.Username?.Trim() ?? string.Empty;
        var invalid = new LoginResponseDto { Succeeded = false, Message = "שם המשתמש או הסיסמה שגויים." };

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(request?.Password))
        {
            return invalid;
        }

        var user = _users.FindByUsername(username);
        if (user is null ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request!.Password) ==
            PasswordVerificationResult.Failed)
        {
            return invalid;
        }

        if (user.IsBlocked)
        {
            return new LoginResponseDto { Succeeded = false, Message = "החשבון חסום. פנה למנהל המערכת." };
        }

        return new LoginResponseDto
        {
            Succeeded = true,
            Message = "התחברת בהצלחה.",
            SessionToken = _sessions.Create(user.UserId),
            Username = user.Username,
            Role = user.Role
        };
    }

    public LoginResponseDto GetCurrentSession(string sessionToken)
    {
        return TryGetAuthenticatedUser(sessionToken, out var user)
            ? new LoginResponseDto
            {
                Succeeded = true,
                Username = user.Username,
                Role = user.Role,
                Message = "החיבור פעיל."
            }
            : new LoginResponseDto { Succeeded = false, Message = "פג תוקף החיבור. יש להתחבר מחדש." };
    }

    public OperationResultDto Logout(string sessionToken)
    {
        _sessions.Remove(sessionToken);
        return Success("התנתקת.");
    }

    public ManagedUsersResponseDto GetManagedUsers(string sessionToken)
    {
        if (!TryGetAdmin(sessionToken))
        {
            return new ManagedUsersResponseDto
            {
                Succeeded = false,
                Message = "אין הרשאה. נדרשת התחברות כמנהל."
            };
        }

        return new ManagedUsersResponseDto
        {
            Succeeded = true,
            Message = "רשימת המשתמשים נטענה.",
            Users = _users.GetAll().ToList()
        };
    }

    public OperationResultDto CreateManagedUser(string sessionToken, CreateManagedUserRequestDto request)
    {
        if (!TryGetAdmin(sessionToken))
        {
            return Failure("אין הרשאה. נדרשת התחברות כמנהל.");
        }

        var username = request?.Username?.Trim() ?? string.Empty;
        var role = request?.Role?.Trim() ?? string.Empty;
        var usernameError = AccountRules.ValidateUsername(username);
        if (usernameError is not null)
        {
            return Failure(usernameError);
        }

        var passwordError = AccountRules.ValidatePassword(request?.Password);
        if (passwordError is not null)
        {
            return Failure(passwordError);
        }

        if (role is not ("Player" or "Manager" or "Admin"))
        {
            return Failure("התפקיד שנבחר אינו תקין.");
        }

        var account = new UserRecord { Username = username, Role = role };
        var hash = _passwordHasher.HashPassword(account, request!.Password);
        try
        {
            return _users.TryCreateManagedAccount(username, role, hash)
                ? Success($"חשבון {username} נוצר.")
                : Failure("שם המשתמש כבר קיים.");
        }
        catch
        {
            return Failure("לא ניתן ליצור את החשבון כרגע.");
        }
    }

    public OperationResultDto DeleteManagedUser(string sessionToken, int userId)
    {
        if (!TryGetAdmin(sessionToken))
        {
            return Failure("אין הרשאה. נדרשת התחברות כמנהל.");
        }

        var target = _users.FindById(userId);
        if (target is null)
        {
            return Failure("המשתמש לא נמצא.");
        }

        if (target.Role == "Admin" && !target.IsBlocked && _users.CountActiveAdmins() <= 1)
        {
            return Failure("אי אפשר למחוק את המנהל הפעיל האחרון.");
        }

        return _users.DeleteUser(userId)
            ? Success($"החשבון של {target.Username} נמחק.")
            : Failure("לא הצלחתי למחוק את החשבון.");
    }

    public OperationResultDto UpdateUserRole(string sessionToken, int userId, string role)
    {
        if (!TryGetAdmin(sessionToken))
        {
            return Failure("אין הרשאה. נדרשת התחברות כמנהל.");
        }

        if (role is not ("Player" or "Manager" or "Admin"))
        {
            return Failure("התפקיד שנבחר אינו תקין.");
        }

        var target = _users.FindById(userId);
        if (target is null)
        {
            return Failure("המשתמש לא נמצא.");
        }

        if (target.Role == "Admin" && role != "Admin" && !target.IsBlocked && _users.CountActiveAdmins() <= 1)
        {
            return Failure("אי אפשר להסיר את תפקיד המנהל האחרון הפעיל.");
        }

        if (!_users.UpdateRole(userId, role))
        {
            return Failure("לא הצלחתי לעדכן את התפקיד.");
        }

        return Success($"התפקיד של {target.Username} עודכן.");
    }

    public OperationResultDto SetUserBlocked(string sessionToken, int userId, bool isBlocked)
    {
        if (!TryGetAdmin(sessionToken))
        {
            return Failure("אין הרשאה. נדרשת התחברות כמנהל.");
        }

        var target = _users.FindById(userId);
        if (target is null)
        {
            return Failure("המשתמש לא נמצא.");
        }

        if (isBlocked && target.Role == "Admin" && !target.IsBlocked && _users.CountActiveAdmins() <= 1)
        {
            return Failure("אי אפשר לחסום את המנהל הפעיל האחרון.");
        }

        if (!_users.SetBlocked(userId, isBlocked))
        {
            return Failure("לא הצלחתי לעדכן את מצב המשתמש.");
        }

        return Success(isBlocked
            ? $"המשתמש {target.Username} נחסם."
            : $"החסימה של {target.Username} בוטלה.");
    }

    public PlayersResponseDto GetPlayers(string searchText, string position, int minimumRating, int maximumRating)
    {
        if (minimumRating < 0 || maximumRating > 100 || minimumRating > maximumRating)
        {
            return new PlayersResponseDto { Succeeded = false, Message = "טווח הדירוג אינו תקין." };
        }

        return new PlayersResponseDto
        {
            Succeeded = true,
            Message = "רשימת השחקנים נטענה.",
            Players = _football.GetPlayers(searchText ?? string.Empty, position ?? string.Empty, minimumRating, maximumRating).ToList()
        };
    }

    public TeamsResponseDto GetTeams() => new()
    {
        Succeeded = true,
        Message = "רשימת הקבוצות נטענה.",
        Teams = _football.GetTeams().ToList()
    };

    public PlayersResponseDto GetTeamRoster(int teamId)
    {
        if (teamId <= 0)
        {
            return new PlayersResponseDto { Succeeded = false, Message = "הקבוצה שנבחרה אינה תקינה." };
        }

        return new PlayersResponseDto
        {
            Succeeded = true,
            Message = "הסגל נטען.",
            Players = _football.GetTeamRoster(teamId).ToList()
        };
    }

    public PlayersResponseDto GetMyFavorites(string sessionToken)
    {
        if (!TryGetUserWithRole(sessionToken, "Player", out var player))
        {
            return new PlayersResponseDto { Succeeded = false, Message = "נדרשת התחברות כשחקן." };
        }

        return new PlayersResponseDto
        {
            Succeeded = true,
            Message = "המועדפים שלך נטענו.",
            Players = _football.GetFavorites(player.UserId).ToList()
        };
    }

    public OperationResultDto AddFavorite(string sessionToken, int playerId)
    {
        if (!TryGetUserWithRole(sessionToken, "Player", out _))
        {
            return Failure("נדרשת התחברות כשחקן.");
        }

        if (playerId <= 0)
        {
            return Failure("השחקן שנבחר אינו תקין.");
        }

        try
        {
            _football.AddFavorite(_sessions.FindUserId(sessionToken)!.Value, playerId);
            return Success("השחקן נוסף למועדפים.");
        }
        catch
        {
            return Failure("לא ניתן להוסיף את השחקן. ייתכן שהוא כבר במועדפים או שאינו קיים.");
        }
    }

    public OperationResultDto RemoveFavorite(string sessionToken, int playerId)
    {
        if (!TryGetUserWithRole(sessionToken, "Player", out var player))
        {
            return Failure("נדרשת התחברות כשחקן.");
        }

        return _football.RemoveFavorite(player.UserId, playerId)
            ? Success("השחקן הוסר מהמועדפים.")
            : Failure("השחקן לא נמצא ברשימת המועדפים שלך.");
    }

    public OperationResultDto SavePlayer(string sessionToken, PlayerDto player)
    {
        if (!TryGetUserWithRole(sessionToken, "Manager", out _))
        {
            return Failure("נדרשת התחברות כמאמן כדי לנהל שחקנים.");
        }

        var validationError = ValidatePlayer(player);
        if (validationError is not null)
        {
            return Failure(validationError);
        }

        try
        {
            return _football.SavePlayer(player)
                ? Success(player.PlayerId == 0 ? "השחקן נוסף." : "פרטי השחקן עודכנו.")
                : Failure("השחקן לא נמצא.");
        }
        catch
        {
            return Failure("לא ניתן לשמור את פרטי השחקן.");
        }
    }

    public OperationResultDto DeletePlayer(string sessionToken, int playerId)
    {
        if (!TryGetUserWithRole(sessionToken, "Manager", out _))
        {
            return Failure("נדרשת התחברות כמאמן כדי לנהל שחקנים.");
        }

        try
        {
            return _football.DeletePlayer(playerId)
                ? Success("השחקן נמחק.")
                : Failure("השחקן לא נמצא.");
        }
        catch
        {
            return Failure("לא ניתן למחוק שחקן שמשויך לסטטיסטיקה או להעברה קיימת.");
        }
    }

    public OperationResultDto SaveTeam(string sessionToken, TeamDto team)
    {
        if (!TryGetUserWithRole(sessionToken, "Manager", out _))
        {
            return Failure("נדרשת התחברות כמאמן כדי לנהל קבוצות.");
        }

        var validationError = ValidateTeam(team);
        if (validationError is not null)
        {
            return Failure(validationError);
        }

        try
        {
            return _football.SaveTeam(team)
                ? Success(team.TeamId == 0 ? "הקבוצה נוספה." : "פרטי הקבוצה עודכנו.")
                : Failure("הקבוצה לא נמצאה.");
        }
        catch
        {
            return Failure("לא ניתן לשמור את פרטי הקבוצה.");
        }
    }

    public OperationResultDto DeleteTeam(string sessionToken, int teamId)
    {
        if (!TryGetUserWithRole(sessionToken, "Manager", out _))
        {
            return Failure("נדרשת התחברות כמאמן כדי לנהל קבוצות.");
        }

        try
        {
            return _football.DeleteTeam(teamId)
                ? Success("הקבוצה נמחקה.")
                : Failure("הקבוצה לא נמצאה.");
        }
        catch
        {
            return Failure("לא ניתן למחוק קבוצה שמופיעה במשחק או בהעברה קיימת.");
        }
    }

    public OperationResultDto AddPlayerToTeam(string sessionToken, int teamId, int playerId)
    {
        if (!TryGetUserWithRole(sessionToken, "Manager", out _))
        {
            return Failure("נדרשת התחברות כמאמן כדי לנהל סגלים.");
        }

        if (teamId <= 0 || playerId <= 0)
        {
            return Failure("צריך לבחור קבוצה ושחקן תקינים.");
        }

        try
        {
            _football.AddPlayerToTeam(teamId, playerId);
            return Success("השחקן נוסף לסגל.");
        }
        catch
        {
            return Failure("לא ניתן לשייך את השחקן. בדוק שהשחקן והקבוצה קיימים ושהוא עדיין לא משויך.");
        }
    }

    public OperationResultDto RemovePlayerFromTeam(string sessionToken, int teamId, int playerId)
    {
        if (!TryGetUserWithRole(sessionToken, "Manager", out _))
        {
            return Failure("נדרשת התחברות כמאמן כדי לנהל סגלים.");
        }

        return _football.RemovePlayerFromTeam(teamId, playerId)
            ? Success("השחקן הוסר מהסגל.")
            : Failure("השחקן לא נמצא בסגל הזה.");
    }

    private bool TryGetAdmin(string token)
    {
        return TryGetUserWithRole(token, "Admin", out _);
    }

    private bool TryGetUserWithRole(string token, string role, out UserRecord user)
    {
        if (!TryGetAuthenticatedUser(token, out user))
        {
            return false;
        }

        return user.Role == role;
    }

    private bool TryGetAuthenticatedUser(string token, out UserRecord user)
    {
        var userId = _sessions.FindUserId(token);
        var currentUser = userId is null ? null : _users.FindById(userId.Value);
        if (currentUser is null || currentUser.IsBlocked)
        {
            _sessions.Remove(token);
            user = new UserRecord();
            return false;
        }

        user = currentUser;
        return true;
    }

    private static string? ValidatePlayer(PlayerDto? player)
    {
        if (player is null || string.IsNullOrWhiteSpace(player.FirstName) || string.IsNullOrWhiteSpace(player.LastName))
        {
            return "יש להזין שם פרטי ושם משפחה.";
        }

        if (player.FirstName.Trim().Length > 50 || player.LastName.Trim().Length > 50 ||
            string.IsNullOrWhiteSpace(player.Position) || player.Position.Trim().Length > 30)
        {
            return "אחד מפרטי השחקן ארוך מהמותר.";
        }

        if (player.Age < 1 || player.Age > 120 ||
            new[] { player.Rating, player.Speed, player.Shooting, player.Passing, player.Defense }
                .Any(value => value is < 0 or > 100))
        {
            return "הגיל או אחד הדירוגים אינם בטווח המותר.";
        }

        player.FirstName = player.FirstName.Trim();
        player.LastName = player.LastName.Trim();
        player.Position = player.Position.Trim();
        return null;
    }

    private static string? ValidateTeam(TeamDto? team)
    {
        if (team is null || string.IsNullOrWhiteSpace(team.Name) ||
            string.IsNullOrWhiteSpace(team.Country) || string.IsNullOrWhiteSpace(team.League))
        {
            return "יש להזין שם קבוצה, מדינה וליגה.";
        }

        if (team.Name.Trim().Length > 100 || team.Country.Trim().Length > 60 || team.League.Trim().Length > 60)
        {
            return "אחד מפרטי הקבוצה ארוך מהמותר.";
        }

        if (team.Budget < 0)
        {
            return "תקציב הקבוצה אינו יכול להיות שלילי.";
        }

        team.Name = team.Name.Trim();
        team.Country = team.Country.Trim();
        team.League = team.League.Trim();
        return null;
    }

    private static OperationResultDto Success(string message) =>
        new() { Succeeded = true, Message = message };

    private static OperationResultDto Failure(string message) =>
        new() { Succeeded = false, Message = message };
}
