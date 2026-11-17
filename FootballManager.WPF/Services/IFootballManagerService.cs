using System.ServiceModel;
using FootballManager.Contracts;

namespace FootballManager.WPF.Services;

/// <summary>חוזה WCF שמפרט את פעולות החשבון, התוכן, המשחקים ו־C4. WPF משתמש בחתימות כדי לקרוא לשירות; FootballManagerService מממש אותן בשרת.</summary>
[ServiceContract(Name = "IFootballManagerService", Namespace = "urn:footballmanager:services")]
public interface IFootballManagerService
{
    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Transfers ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Get Transfers, Get Transfers Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetTransfers")]
    TransfersResponseDto GetTransfers(string sessionToken);

    /// <summary>מבצעת את פעולת הנתונים Save Transfer. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Transfer Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "SaveTransfer")]
    OperationResultDto SaveTransfer(string sessionToken, TransferDto transfer);

    /// <summary>מבצעת את פעולת הנתונים Delete Transfer. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Transfer Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transferId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "DeleteTransfer")]
    OperationResultDto DeleteTransfer(string sessionToken, int transferId);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Player Statistics ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Get Player Statistics Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetPlayerStatistics")]
    PlayerStatisticsResponseDto GetPlayerStatistics(string sessionToken);

    /// <summary>מבצעת את פעולת הנתונים Save Player Statistic. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Player Statistic Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statistic">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "SavePlayerStatistic")]
    OperationResultDto SavePlayerStatistic(string sessionToken, PlayerStatisticDto statistic);

    /// <summary>מבצעת את פעולת הנתונים Delete Player Statistic. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Player Statistic Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statisticId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "DeletePlayerStatistic")]
    OperationResultDto DeletePlayerStatistic(string sessionToken, int statisticId);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Matches ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Get Matches, Get Matches Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="searchText">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="matchDate">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetMatches")]
    MatchesResponseDto GetMatches(string sessionToken, string searchText, DateTime? matchDate);

    /// <summary>מבצעת את פעולת הנתונים Save Match. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Match Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="match">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "SaveMatch")]
    OperationResultDto SaveMatch(string sessionToken, MatchDto match);

    /// <summary>מבצעת את פעולת הנתונים Delete Match. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Match Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="matchId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "DeleteMatch")]
    OperationResultDto DeleteMatch(string sessionToken, int matchId);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Service Status ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Map Football Manager Web Api, Get Service Status Async.</summary>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetServiceStatus")]
    ServiceStatusDto GetServiceStatus();

    /// <summary>מבצעת את שלב המערכת Register באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Map Football Manager Web Api, Register Async.</summary>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "Register")]
    OperationResultDto Register(RegisterRequestDto request);

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות. נקראת בקוד מתוך Map Football Manager Web Api, Login Async.</summary>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "Login")]
    LoginResponseDto Login(LoginRequestDto request);

    /// <summary>מסיימת את ההפעלה המחוברת ומנקה את מצב המשתמש בצד הלקוח. נקראת בקוד מתוך Map Football Manager Web Api, Logout Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "Logout")]
    OperationResultDto Logout(string sessionToken);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Managed Users ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Map Football Manager Web Api, Get Managed Users Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetManagedUsers")]
    ManagedUsersResponseDto GetManagedUsers(string sessionToken);

    /// <summary>מבצעת את פעולת הנתונים Create Managed User. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Map Football Manager Web Api, Create Managed User Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "CreateManagedUser")]
    OperationResultDto CreateManagedUser(string sessionToken, CreateManagedUserRequestDto request);

    /// <summary>מבצעת את פעולת הנתונים Delete Managed User. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Map Football Manager Web Api, Delete Managed User Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "DeleteManagedUser")]
    OperationResultDto DeleteManagedUser(string sessionToken, int userId);

    /// <summary>מבצעת את פעולת הנתונים Update User Role. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Map Football Manager Web Api, Update User Role Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "UpdateUserRole")]
    OperationResultDto UpdateUserRole(string sessionToken, int userId, string role);

    /// <summary>מבצעת את פעולת הנתונים Set User Blocked. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Map Football Manager Web Api, Set User Blocked Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="isBlocked">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "SetUserBlocked")]
    OperationResultDto SetUserBlocked(string sessionToken, int userId, bool isBlocked);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Players ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Seed Demo Content, Get Players.</summary>
    /// <param name="searchText">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="position">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="minimumRating">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="maximumRating">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetPlayers")]
    PlayersResponseDto GetPlayers(string searchText, string position, int minimumRating, int maximumRating);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Teams ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Seed Demo Content, Get Teams.</summary>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetTeams")]
    TeamsResponseDto GetTeams();

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Team Roster ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Seed Demo Content, Get Team Roster.</summary>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetTeamRoster")]
    PlayersResponseDto GetTeamRoster(int teamId);

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get My Favorites ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Map Football Manager Web Api, Get My Favorites Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "GetMyFavorites")]
    PlayersResponseDto GetMyFavorites(string sessionToken);

    /// <summary>מבצעת את פעולת הנתונים Add Favorite. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Add Favorite, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "AddFavorite")]
    OperationResultDto AddFavorite(string sessionToken, int playerId);

    /// <summary>מבצעת את פעולת הנתונים Remove Favorite. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Remove Favorite, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "RemoveFavorite")]
    OperationResultDto RemoveFavorite(string sessionToken, int playerId);

    /// <summary>מבצעת את פעולת הנתונים Save Player. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Seed Demo Content, Save Player.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="player">נתוני השחקן או מזהה השחקן.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "SavePlayer")]
    OperationResultDto SavePlayer(string sessionToken, PlayerDto player);

    /// <summary>מבצעת את פעולת הנתונים Delete Player. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Player, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "DeletePlayer")]
    OperationResultDto DeletePlayer(string sessionToken, int playerId);

    /// <summary>מבצעת את פעולת הנתונים Save Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Seed Demo Content, Save Team.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="team">נתוני הקבוצה או מזהה הקבוצה.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "SaveTeam")]
    OperationResultDto SaveTeam(string sessionToken, TeamDto team);

    /// <summary>מבצעת את פעולת הנתונים Delete Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Team, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "DeleteTeam")]
    OperationResultDto DeleteTeam(string sessionToken, int teamId);

    /// <summary>מבצעת את פעולת הנתונים Add Player To Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Seed Demo Content, Add Player To Team.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "AddPlayerToTeam")]
    OperationResultDto AddPlayerToTeam(string sessionToken, int teamId, int playerId);

    /// <summary>מבצעת את פעולת הנתונים Remove Player From Team. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Remove Player From Team, Map Football Manager Web Api.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    [OperationContract(Name = "RemovePlayerFromTeam")]
    OperationResultDto RemovePlayerFromTeam(string sessionToken, int teamId, int playerId);
}
