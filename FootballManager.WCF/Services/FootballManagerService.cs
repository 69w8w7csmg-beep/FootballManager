using CoreWCF;
using CoreWCF.Channels;
using System.Net;
using Microsoft.AspNetCore.Identity;
using FootballManager.Contracts;
using FootballManager.WCF.Contracts;
using FootballManager.WCF.Data;

namespace FootballManager.WCF.Services;

/// <summary>מימוש חוזה SOAP/WCF בצד השרת. הוא מקבל DTOs מהלקוח, מאמת חשבונות ו־Session, בודק הרשאות ושולח פעולות למאגרי Access; שכבת WPF אינה פונה למסד.</summary>
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
    // ב־REST קוראים לשירות ישירות. המתאם משתמש בקוד של מופע הבקשה כדי להחזיר אותה חסימה גם באתר.
    internal HttpStatusCode ResponseStatus { get; private set; } = HttpStatusCode.OK;

    /// <summary>מבצעת את שלב המערכת Football Manager Service באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="database">ערך מסוג AccessDatabase שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="users">ערך מסוג AccessUserRepository שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="football">ערך מסוג AccessFootballRepository שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="matches">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <param name="transfers">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <param name="statistics">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <param name="passwordHasher">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <param name="sessions">ערך מסוג LoginSessionStore שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public FootballManagerService(
        AccessDatabase database,
        AccessUserRepository users,
        AccessFootballRepository football,
        AccessMatchRepository matches,
        AccessTransferRepository transfers,
        AccessPlayerStatisticRepository statistics,
        PasswordHasher<UserRecord> passwordHasher,
        LoginSessionStore sessions,
        PlayerAnalysisProvider playerAnalysis,
        GeminiChatProvider geminiChat,
        AiChatHistoryStore chatHistory)
    {
        _database = database;
        _users = users;
        _football = football;
        _matches = matches;
        _transfers = transfers;
        _statistics = statistics;
        _passwordHasher = passwordHasher;
        _sessions = sessions;
        _playerAnalysis = playerAnalysis;
        _geminiChat = geminiChat;
        _chatHistory = chatHistory;
    }

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Service Status ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Map Football Manager Web Api, Get Service Status Async.</summary>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>נקראת מחוזה WCF בעת הרשמה. היא מנקה ובודקת את שם המשתמש והסיסמה, יוצרת חשבון Player בלבד, מגבבת את הסיסמה ב־PasswordHasher ושומרת דרך AccessUserRepository. שם כפול ושגיאת מסד מוחזרים כתשובה בלי ליצור חשבון.</summary>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto Register(RegisterRequestDto request)
    {
        // השירות מאמת מחדש את הנתונים גם אם הלקוח כבר הציג בדיקת קלט.
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

        // סיסמה נשמרת כגיבוב, ולא כטקסט המקורי שנשלח מהמסך.
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

    /// <summary>מחפשת חשבון לפי שם משתמש, מאמתת את הסיסמה מול PasswordHash ובודקת חסימה. בהצלחה יוצרת אסימון Session ומחזירה שם ותפקיד; פרטים שגויים או חשבון חסום אינם מקבלים אסימון.</summary>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public LoginResponseDto Login(LoginRequestDto request)
    {
        var username = request?.Username?.Trim() ?? string.Empty;
        var invalid = new LoginResponseDto { Succeeded = false, Message = "שם המשתמש או הסיסמה שגויים." };

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(request?.Password))
        {
            return invalid;
        }

        // השוואת הסיסמה מתבצעת מול הגיבוב באמצעות PasswordHasher.
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

        // האסימון האקראי משמש בבקשות הבאות לזיהוי המשתמש בצד השרת.
        return new LoginResponseDto
        {
            Succeeded = true,
            Message = "התחברת בהצלחה.",
            SessionToken = _sessions.Create(user.UserId),
            Username = user.Username,
            Role = user.Role
        };
    }

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Current Session ומחזירה את התוצאה לשכבה הקוראת. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public LoginResponseDto GetCurrentSession(string sessionToken)
    {
        if (!TryGetAuthenticatedUser(sessionToken, out var user))
        {
            SetAccessDenial(HttpStatusCode.Unauthorized);
            return new LoginResponseDto { Message = "פג תוקף החיבור. יש להתחבר מחדש." };
        }

        return new LoginResponseDto
        {
            Succeeded = true,
            Username = user.Username,
            Role = user.Role,
            Message = "החיבור פעיל."
        };
    }

    /// <summary>מסיימת את ההפעלה המחוברת ומנקה את מצב המשתמש בצד הלקוח. נקראת בקוד מתוך Map Football Manager Web Api, Logout Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto Logout(string sessionToken)
    {
        _sessions.Remove(sessionToken);
        return Success("התנתקת.");
    }

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Managed Users ומחזירה את התוצאה לשכבה הקוראת. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Map Football Manager Web Api, Get Managed Users Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Create Managed User. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Map Football Manager Web Api, Create Managed User Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Delete Managed User. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Map Football Manager Web Api, Delete Managed User Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Update User Role. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Map Football Manager Web Api, Update User Role Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Set User Blocked. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Map Football Manager Web Api, Set User Blocked Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="isBlocked">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Players ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Seed Demo Content, Get Players.</summary>
    /// <param name="searchText">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="position">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="minimumRating">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="maximumRating">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Teams ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Seed Demo Content, Get Teams.</summary>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public TeamsResponseDto GetTeams() => new()
    {
        Succeeded = true,
        Message = "רשימת הקבוצות נטענה.",
        Teams = _football.GetTeams().ToList()
    };

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Team Roster ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Seed Demo Content, Get Team Roster.</summary>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get My Favorites ומחזירה את התוצאה לשכבה הקוראת. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Map Football Manager Web Api, Get My Favorites Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Add Favorite. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Add Favorite, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    public OperationResultDto AddFavorite(string sessionToken, int playerId)
    {
        if (!TryGetUserWithRole(sessionToken, "Player", out var owner))
        {
            return Failure("נדרשת התחברות כשחקן.");
        }

        if (playerId <= 0)
        {
            return Failure("השחקן שנבחר אינו תקין.");
        }

        try
        {
            _football.AddFavorite(owner.UserId, playerId);
            return Success("השחקן נוסף למועדפים.");
        }
        catch
        {
            return Failure("לא ניתן להוסיף את השחקן. ייתכן שהוא כבר במועדפים או שאינו קיים.");
        }
    }

    /// <summary>מבצעת את פעולת הנתונים Remove Favorite. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Remove Favorite, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Save Player. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Seed Demo Content, Save Player.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="player">נתוני השחקן או מזהה השחקן.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Delete Player. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Delete Player, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Save Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Seed Demo Content, Save Team.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="team">נתוני הקבוצה או מזהה הקבוצה.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Delete Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Delete Team, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Add Player To Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת; מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Seed Demo Content, Add Player To Team.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Remove Player From Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Remove Player From Team, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
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

    /// <summary>מנסה לבצע את הפעולה Try Get Admin ומחזירה תוצאה שמאפשרת לקוד הקורא לזהות הצלחה או כישלון. בגוף הפעולה: בודקת זהות והרשאות בשרת. נקראת בקוד מתוך Get Managed Users, Create Managed User.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    private bool TryGetAdmin(string token)
    {
        return TryGetUserWithRole(token, "Admin", out _);
    }

    /// <summary>בודקת קודם שהאסימון משויך לחשבון פעיל ואז משווה את ה־Role שנשמר באותו חשבון לתפקיד הנדרש. המתודות המוגנות בשרת קוראות לה; היא אינה מקבלת תפקיד מהלקוח.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <param name="user">ערך מסוג UserRecord שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    private bool TryGetUserWithRole(string token, string role, out UserRecord user)
    {
        if (!TryGetAuthenticatedUser(token, out user))
        {
            SetAccessDenial(HttpStatusCode.Unauthorized);
            return false;
        }

        if (user.Role == role) return true;

        SetAccessDenial(HttpStatusCode.Forbidden);
        return false;
    }

    /// <summary>קובעת את סטטוס HTTP של דחיית הבקשה. ב־SOAP הקוד נכתב בתגובה היוצאת, ובאתר מתאם REST קורא אותו ממופע השירות של הבקשה.</summary>
    /// <param name="status">401 לחיבור חסר או לא תקף, 403 למשתמש פעיל שחסרה לו הרשאה.</param>
    private void SetAccessDenial(HttpStatusCode status)
    {
        ResponseStatus = status;
        if (OperationContext.Current is { } context)
        {
            context.OutgoingMessageProperties[HttpResponseMessageProperty.Name] =
                new HttpResponseMessageProperty { StatusCode = status };
        }
    }

    /// <summary>מאתרת מזהה משתמש מתוך LoginSessionStore, טוענת את הרשומה מ־Access ודוחה Session שפג או חשבון חסום. מחזירה UserRecord פעיל או false, ומשותפת לבדיקות התפקידים.</summary>
    /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="user">ערך מסוג UserRecord שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    private bool TryGetAuthenticatedUser(string token, out UserRecord user)
    {
        // האסימון נבדק בשרת מול מאגר ה־Sessions ואז נשלף המשתמש ממסד הנתונים.
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

    /// <summary>בודקת את תקינות הנתונים עבור Validate Player ומחזירה את תוצאת הבדיקה כדי למנוע המשך עם קלט שגוי. נקראת בקוד מתוך Save Player.</summary>
    /// <param name="player">נתוני השחקן או מזהה השחקן.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
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

    /// <summary>בודקת את תקינות הנתונים עבור Validate Team ומחזירה את תוצאת הבדיקה כדי למנוע המשך עם קלט שגוי. נקראת בקוד מתוך Save Team.</summary>
    /// <param name="team">נתוני הקבוצה או מזהה הקבוצה.</param>
    /// <returns>הודעת שגיאה אם הבדיקה נכשלה, או null אם הנתונים תקינים.</returns>
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

    /// <summary>מבצעת את שלב המערכת Success באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Register, Logout.</summary>
    /// <param name="message">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    private static OperationResultDto Success(string message) =>
        new() { Succeeded = true, Message = message };

    /// <summary>מבצעת את שלב המערכת Failure באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Register, Create Managed User.</summary>
    /// <param name="message">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    private static OperationResultDto Failure(string message) =>
        new() { Succeeded = false, Message = message };
}
