using System.ServiceModel;
using FootballManager.Contracts;

namespace FootballManager.WPF.Services;

/// <summary>מתאם בצד WPF שמפעיל פעולות בחוזה WCF ומחזיר DTOs ומשימות אסינכרוניות ל־ViewModels ולחלונות. הוא אינו מחזיק חיבור ל־Access.</summary>
public sealed class WcfServiceClient
{
    private const string ServiceAddress = "http://localhost:5152/FootballManagerService";

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Transfers Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Load Transfers Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<TransfersResponseDto> GetTransfersAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetTransfers(sessionToken),
            message => new TransfersResponseDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Save Transfer Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Transfer Async, Player Transfer Attempt_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> SaveTransferAsync(string sessionToken, TransferDto transfer) =>
        InvokeRoleProtectedAsync(service => service.SaveTransfer(sessionToken, transfer),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Delete Transfer Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Transfer Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transferId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> DeleteTransferAsync(string sessionToken, int transferId) =>
        InvokeRoleProtectedAsync(service => service.DeleteTransfer(sessionToken, transferId),
            message => new OperationResultDto { Message = message });

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Player Statistics Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Load Statistics Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<PlayerStatisticsResponseDto> GetPlayerStatisticsAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetPlayerStatistics(sessionToken),
            message => new PlayerStatisticsResponseDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Save Player Statistic Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Statistic Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statistic">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> SavePlayerStatisticAsync(string sessionToken, PlayerStatisticDto statistic) =>
        InvokeRoleProtectedAsync(service => service.SavePlayerStatistic(sessionToken, statistic),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Delete Player Statistic Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Statistic Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statisticId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> DeletePlayerStatisticAsync(string sessionToken, int statisticId) =>
        InvokeRoleProtectedAsync(service => service.DeletePlayerStatistic(sessionToken, statisticId),
            message => new OperationResultDto { Message = message });

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Matches Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Initialize Async, Load Matches Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="searchText">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="matchDate">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<MatchesResponseDto> GetMatchesAsync(string sessionToken, string searchText = "", DateTime? matchDate = null) =>
        InvokeRoleProtectedAsync(service => service.GetMatches(sessionToken, searchText, matchDate),
            message => new MatchesResponseDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Save Match Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Async, Try Save Result Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="match">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> SaveMatchAsync(string sessionToken, MatchDto match) =>
        InvokeRoleProtectedAsync(service => service.SaveMatch(sessionToken, match),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Delete Match Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="matchId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> DeleteMatchAsync(string sessionToken, int matchId) =>
        InvokeRoleProtectedAsync(service => service.DeleteMatch(sessionToken, matchId),
            message => new OperationResultDto { Message = message });

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Service Status Async ומחזירה את התוצאה לשכבה הקוראת.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<ServiceStatusDto> GetServiceStatusAsync() =>
        Task.Run(() => Invoke(service => service.GetServiceStatus()));

    /// <summary>מבצעת את שלב המערכת Register Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Register_Click.</summary>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> RegisterAsync(RegisterRequestDto request) =>
        Task.Run(() => Invoke(service => service.Register(request)));

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות. נקראת בקוד מתוך Login_Click.</summary>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<LoginResponseDto> LoginAsync(LoginRequestDto request) =>
        Task.Run(() => Invoke(service => service.Login(request)));

    /// <summary>מסיימת את ההפעלה המחוברת ומנקה את מצב המשתמש בצד הלקוח. נקראת בקוד מתוך Logout_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> LogoutAsync(string sessionToken) =>
        Task.Run(() => Invoke(service => service.Logout(sessionToken)));

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Managed Users Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Refresh Managed Users Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<ManagedUsersResponseDto> GetManagedUsersAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetManagedUsers(sessionToken),
            message => new ManagedUsersResponseDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Create Managed User Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Create Managed User_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="request">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> CreateManagedUserAsync(
        string sessionToken,
        CreateManagedUserRequestDto request) =>
        InvokeRoleProtectedAsync(service => service.CreateManagedUser(sessionToken, request),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Delete Managed User Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Managed User_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> DeleteManagedUserAsync(string sessionToken, int userId) =>
        InvokeRoleProtectedAsync(service => service.DeleteManagedUser(sessionToken, userId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Update User Role Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Change Role_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> UpdateUserRoleAsync(string sessionToken, int userId, string role) =>
        InvokeRoleProtectedAsync(service => service.UpdateUserRole(sessionToken, userId, role),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Set User Blocked Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Block User_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="isBlocked">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> SetUserBlockedAsync(string sessionToken, int userId, bool isBlocked) =>
        InvokeRoleProtectedAsync(service => service.SetUserBlocked(sessionToken, userId, isBlocked),
            message => new OperationResultDto { Message = message });

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Players Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Initialize Async, Refresh Public Players Async.</summary>
    /// <param name="searchText">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="position">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="minimumRating">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="maximumRating">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<PlayersResponseDto> GetPlayersAsync(
        string searchText = "",
        string position = "",
        int minimumRating = 0,
        int maximumRating = 100) =>
        Task.Run(() => Invoke(service => service.GetPlayers(searchText, position, minimumRating, maximumRating)));

    /// <summary>שולח בקשת ניתוח לשירות WCF. אסימון המשתמש נבדק בהרשאות השרת; 401 ו־403 מוחזרים כתשובת מצב למסך.</summary>
    /// <param name="sessionToken">אסימון ההתחברות שהתקבל מהשרת.</param>
    /// <param name="playerId">מזהה השחקן הציבורי לניתוח.</param>
    /// <returns>משימה שמחזירה את תשובת הניתוח מהשרת.</returns>
    public Task<PlayerAnalysisResponseDto> AnalyzePlayerAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.AnalyzePlayer(sessionToken, playerId),
            message => new PlayerAnalysisResponseDto { Message = message });

    /// <summary>מעבירה שאלה או איפוס דרך חוזה WCF ומחזירה תשובת שרת או הודעת חסימה.</summary>
    public Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request) =>
        InvokeRoleProtectedAsync(service => service.Chat(request),
            message => new AiChatResponseDto { Message = message });

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Teams Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Initialize Async, Refresh Public Teams Async.</summary>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<TeamsResponseDto> GetTeamsAsync() =>
        Task.Run(() => Invoke(service => service.GetTeams()));

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get Team Roster Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Public Teams List_Selection Changed, Refresh Manager Roster Async.</summary>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<PlayersResponseDto> GetTeamRosterAsync(int teamId) =>
        Task.Run(() => Invoke(service => service.GetTeamRoster(teamId)));

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get My Favorites Async ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Refresh Favorites Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<PlayersResponseDto> GetMyFavoritesAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetMyFavorites(sessionToken),
            message => new PlayersResponseDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Add Favorite Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Add Favorite_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> AddFavoriteAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.AddFavorite(sessionToken, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Remove Favorite Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Remove Favorite_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> RemoveFavoriteAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.RemoveFavorite(sessionToken, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Save Player Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Player_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="player">נתוני השחקן או מזהה השחקן.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> SavePlayerAsync(string sessionToken, PlayerDto player) =>
        InvokeRoleProtectedAsync(service => service.SavePlayer(sessionToken, player),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Delete Player Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Player_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> DeletePlayerAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.DeletePlayer(sessionToken, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Save Team Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Save Team_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="team">נתוני הקבוצה או מזהה הקבוצה.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> SaveTeamAsync(string sessionToken, TeamDto team) =>
        InvokeRoleProtectedAsync(service => service.SaveTeam(sessionToken, team),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Delete Team Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Delete Team_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> DeleteTeamAsync(string sessionToken, int teamId) =>
        InvokeRoleProtectedAsync(service => service.DeleteTeam(sessionToken, teamId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Add Player To Team Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Add Roster Player_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> AddPlayerToTeamAsync(string sessionToken, int teamId, int playerId) =>
        InvokeRoleProtectedAsync(service => service.AddPlayerToTeam(sessionToken, teamId, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את פעולת הנתונים Remove Player From Team Async. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Remove Roster Player_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    public Task<OperationResultDto> RemovePlayerFromTeamAsync(string sessionToken, int teamId, int playerId) =>
        InvokeRoleProtectedAsync(service => service.RemovePlayerFromTeam(sessionToken, teamId, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מבצעת את שלב המערכת Invoke Role Protected Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Get Transfers Async, Save Transfer Async.</summary>
    /// <param name="operation">ערך מסוג Func&lt;IFootballManagerService, T&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="denied">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
    private static Task<T> InvokeRoleProtectedAsync<T>(Func<IFootballManagerService, T> operation, Func<string, T> denied)
    {
        return Task.Run(() =>
        {
            var http = new ServiceHttpStatus();
            try
            {
                return Invoke(operation, http);
            }
            catch (System.ServiceModel.Security.MessageSecurityException) when (http.Status == System.Net.HttpStatusCode.Unauthorized)
            {
                return denied("401 Unauthorized · נדרשת התחברות פעילה.");
            }
            catch (System.ServiceModel.Security.MessageSecurityException) when (http.Status == System.Net.HttpStatusCode.Forbidden)
            {
                return denied("403 Forbidden · לַחשבון זה אין הרשאה לבצע את הפעולה.");
            }
        });
    }

    /// <summary>מבצעת את שלב המערכת Invoke באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: מטפלת בשגיאה לפי בלוק catch שבגוף הפעולה. נקראת בקוד מתוך Get Service Status Async, Register Async.</summary>
    /// <param name="operation">ערך מסוג Func&lt;IFootballManagerService, T&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="http">ערך מסוג ServiceHttpStatus? שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>ערך מסוג T שמשמש את הקוד הקורא.</returns>
    private static T Invoke<T>(Func<IFootballManagerService, T> operation, ServiceHttpStatus? http = null)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = 1024 * 1024
        };
        var address = new EndpointAddress(ServiceAddress);
        var factory = new ChannelFactory<IFootballManagerService>(binding, address);
        if (http is not null)
        {
            factory.Endpoint.EndpointBehaviors.Add(http);
        }
        var channel = factory.CreateChannel();
        var communication = (ICommunicationObject)channel;

        try
        {
            var result = operation(channel);
            communication.Close();
            factory.Close();
            return result;
        }
        catch
        {
            communication.Abort();
            factory.Abort();
            throw;
        }
    }
}
