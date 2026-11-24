// מטרה: שולח קריאות SOAP מתצוגות WPF לשירות WCF.
// מי משתמש: MainWindow וה־ViewModels.
// פעולות: Invoke, קריאות החשבון, התוכן והבינה והצגת 401 ו־403.
// תלויות: IFootballManagerService, BasicHttpBinding ו־ServiceHttpStatus.
using System.ServiceModel;
using FootballManager.Contracts;

namespace FootballManager.WPF.Services;

/// <summary>מתאם בצד WPF שמפעיל פעולות בחוזה WCF ומחזיר DTOs ומשימות אסינכרוניות ל־ViewModels ולחלונות. הוא אינו מחזיק חיבור ל־Access.</summary>
public sealed class WcfServiceClient
{
    private const string ServiceAddress = "http://localhost:5152/FootballManagerService";

    /// <summary>קורא את ההעברות למאמן. בלי אסימון פעיל השרת מחזיר 401, ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת העברות כאשר הבקשה הצליחה.</returns>
    public Task<TransfersResponseDto> GetTransfersAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetTransfers(sessionToken),
            message => new TransfersResponseDto { Message = message });

    /// <summary>מוסיף העברה אם המזהה 0, או מעדכן העברה קיימת. מיועד למאמן. דחיית התחברות מחזירה 401 ודחיית הרשאה מחזירה 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> SaveTransferAsync(string sessionToken, TransferDto transfer) =>
        InvokeRoleProtectedAsync(service => service.SaveTransfer(sessionToken, transfer),
            message => new OperationResultDto { Message = message });

    /// <summary>מוחק העברה קיימת לפי מזהה. השרת מתיר למאמן בלבד ומחזיר 401 או 403 בדחייה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transferId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> DeleteTransferAsync(string sessionToken, int transferId) =>
        InvokeRoleProtectedAsync(service => service.DeleteTransfer(sessionToken, transferId),
            message => new OperationResultDto { Message = message });

    /// <summary>קורא את סטטיסטיקות סבב 4 למאמן. דחייה מחזירה 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת סטטיסטיקות כאשר הבקשה הצליחה.</returns>
    public Task<PlayerStatisticsResponseDto> GetPlayerStatisticsAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetPlayerStatistics(sessionToken),
            message => new PlayerStatisticsResponseDto { Message = message });

    /// <summary>שומר סטטיסטיקה למשחק לפי מזהה. השרת בודק מאמן, ומחזיר 401 או 403 אם הבקשה חסומה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statistic">נתוני הסטטיסטיקה ששייכים לשחקן ולמשחק.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> SavePlayerStatisticAsync(string sessionToken, PlayerStatisticDto statistic) =>
        InvokeRoleProtectedAsync(service => service.SavePlayerStatistic(sessionToken, statistic),
            message => new OperationResultDto { Message = message });

    /// <summary>מוחק סטטיסטיקה לפי מזהה עבור מאמן. דחייה מחזירה 401 או 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statisticId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> DeletePlayerStatisticAsync(string sessionToken, int statisticId) =>
        InvokeRoleProtectedAsync(service => service.DeletePlayerStatistic(sessionToken, statisticId),
            message => new OperationResultDto { Message = message });

    /// <summary>קורא משחקים לפי חיפוש ותאריך. שחקן ומאמן יכולים לקרוא. אורח מקבל 401 ואדמין מקבל 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="searchText">שם או חלק משם לחיפוש.</param>
    /// <param name="matchDate">תאריך לסינון משחקים, או null לכל התאריכים.</param>
    /// <returns>תוצאת שליפה ורשימת משחקים כאשר הבקשה הצליחה.</returns>
    public Task<MatchesResponseDto> GetMatchesAsync(string sessionToken, string searchText = "", DateTime? matchDate = null) =>
        InvokeRoleProtectedAsync(service => service.GetMatches(sessionToken, searchText, matchDate),
            message => new MatchesResponseDto { Message = message });

    /// <summary>שומר תאריך, קבוצות ותוצאה עבור מאמן. השרת מחזיר 401 ללא כניסה ו־403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="match">נתוני משחק, כולל תאריך, מזהי הקבוצות ושערים.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> SaveMatchAsync(string sessionToken, MatchDto match) =>
        InvokeRoleProtectedAsync(service => service.SaveMatch(sessionToken, match),
            message => new OperationResultDto { Message = message });

    /// <summary>מוחק משחק לפי מזהה עבור מאמן. השרת מחזיר 401 בלי כניסה ו־403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="matchId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> DeleteMatchAsync(string sessionToken, int matchId) =>
        InvokeRoleProtectedAsync(service => service.DeleteMatch(sessionToken, matchId),
            message => new OperationResultDto { Message = message });

    /// <summary>בודק אם השרת רץ ומסד Access מוכן לשימוש. זמין גם לאורח.</summary>
    /// <returns>שם היישום, מצב המסד והודעת השרת.</returns>
    public Task<ServiceStatusDto> GetServiceStatusAsync() =>
        Task.Run(() => Invoke(service => service.GetServiceStatus()));

    /// <summary>פותח חשבון ציבורי מסוג Player לאחר בדיקת השם והסיסמה. השירות מחזיר הודעה על הצלחה או על קלט לא תקין.</summary>
    /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> RegisterAsync(RegisterRequestDto request) =>
        Task.Run(() => Invoke(service => service.Register(request)));

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות. נקראת בקוד מתוך Login_Click.</summary>
    /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
    /// <returns>תוצאה ובה אסימון, שם ותפקיד בהצלחה, או הודעה על שגיאה.</returns>
    public Task<LoginResponseDto> LoginAsync(LoginRequestDto request) =>
        Task.Run(() => Invoke(service => service.Login(request)));

    /// <summary>מסיימת את ההפעלה המחוברת ומנקה את מצב המשתמש בצד הלקוח. נקראת בקוד מתוך Logout_Click.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> LogoutAsync(string sessionToken) =>
        Task.Run(() => Invoke(service => service.Logout(sessionToken)));

    /// <summary>קורא את רשימת המשתמשים עבור Admin. בלי התחברות השרת מחזיר 401 ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת משתמשים בלי סיסמאות.</returns>
    public Task<ManagedUsersResponseDto> GetManagedUsersAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetManagedUsers(sessionToken),
            message => new ManagedUsersResponseDto { Message = message });

    /// <summary>יוצר חשבון בתפקיד שבחר Admin. הסיסמה מגובבת בשרת. חסימה מחזירה 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> CreateManagedUserAsync(
        string sessionToken,
        CreateManagedUserRequestDto request) =>
        InvokeRoleProtectedAsync(service => service.CreateManagedUser(sessionToken, request),
            message => new OperationResultDto { Message = message });

    /// <summary>מוחק חשבון לפי מזהה עבור Admin. השרת שומר על האדמין הפעיל האחרון ומחזיר 401 או 403 בדחיית גישה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> DeleteManagedUserAsync(string sessionToken, int userId) =>
        InvokeRoleProtectedAsync(service => service.DeleteManagedUser(sessionToken, userId),
            message => new OperationResultDto { Message = message });

    /// <summary>משנה תפקיד של חשבון עבור Admin. השרת בודק את התפקיד ואת מצב האדמין האחרון. גישה שנדחית מחזירה 401 או 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> UpdateUserRoleAsync(string sessionToken, int userId, string role) =>
        InvokeRoleProtectedAsync(service => service.UpdateUserRole(sessionToken, userId, role),
            message => new OperationResultDto { Message = message });

    /// <summary>מעדכן חסימה עבור Admin. ללא אסימון מתקבל 401 ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="isBlocked">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> SetUserBlockedAsync(string sessionToken, int userId, bool isBlocked) =>
        InvokeRoleProtectedAsync(service => service.SetUserBlocked(sessionToken, userId, isBlocked),
            message => new OperationResultDto { Message = message });

    /// <summary>קורא את קטלוג השחקנים הציבורי לפי שם, עמדה וטווח דירוג.</summary>
    /// <param name="searchText">שם או חלק משם לחיפוש.</param>
    /// <param name="position">עמדה לסינון, או מחרוזת ריקה לכל העמדות.</param>
    /// <param name="minimumRating">הדירוג הנמוך ביותר במסנן.</param>
    /// <param name="maximumRating">הדירוג הגבוה ביותר במסנן.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
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

    /// <summary>קורא את הקבוצות הציבוריות מ־Access דרך השירות.</summary>
    /// <returns>תוצאת שליפה ורשימת קבוצות כאשר הבקשה הצליחה.</returns>
    public Task<TeamsResponseDto> GetTeamsAsync() =>
        Task.Run(() => Invoke(service => service.GetTeams()));

    /// <summary>קורא את השחקנים המשויכים לקבוצה לפי מזהה קבוצה.</summary>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    public Task<PlayersResponseDto> GetTeamRosterAsync(int teamId) =>
        Task.Run(() => Invoke(service => service.GetTeamRoster(teamId)));

    /// <summary>קורא רק את המועדפים של Player שמזוהה באסימון. בלי התחברות מתקבל 401 ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    public Task<PlayersResponseDto> GetMyFavoritesAsync(string sessionToken) =>
        InvokeRoleProtectedAsync(service => service.GetMyFavorites(sessionToken),
            message => new PlayersResponseDto { Message = message });

    /// <summary>מוסיף שחקן למועדפים של Player שמזוהה באסימון. אין קלט של מזהה הבעלים. השרת מחזיר 401 או 403 בדחייה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> AddFavoriteAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.AddFavorite(sessionToken, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מסיר שחקן מרשימת המועדפים של Player שמזוהה באסימון. גישה שנדחית מחזירה 401 או 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> RemoveFavoriteAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.RemoveFavorite(sessionToken, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מוסיף או מעדכן רשומת שחקן עבור מאמן. השרת מחזיר 401 ללא התחברות או 403 ללא הרשאה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="player">נתוני השחקן או מזהה השחקן.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> SavePlayerAsync(string sessionToken, PlayerDto player) =>
        InvokeRoleProtectedAsync(service => service.SavePlayer(sessionToken, player),
            message => new OperationResultDto { Message = message });

    /// <summary>מוחק שחקן לפי מזהה עבור מאמן. השרת מחזיר 401 או 403 בדחיית גישה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> DeletePlayerAsync(string sessionToken, int playerId) =>
        InvokeRoleProtectedAsync(service => service.DeletePlayer(sessionToken, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מוסיף או מעדכן קבוצה עבור מאמן. השרת מחזיר 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="team">נתוני הקבוצה או מזהה הקבוצה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> SaveTeamAsync(string sessionToken, TeamDto team) =>
        InvokeRoleProtectedAsync(service => service.SaveTeam(sessionToken, team),
            message => new OperationResultDto { Message = message });

    /// <summary>מוחק קבוצה לפי מזהה עבור מאמן. השרת מחזיר 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> DeleteTeamAsync(string sessionToken, int teamId) =>
        InvokeRoleProtectedAsync(service => service.DeleteTeam(sessionToken, teamId),
            message => new OperationResultDto { Message = message });

    /// <summary>משייך שחקן לסגל עבור מאמן. השרת מחזיר 401 או 403 כשהחשבון אינו רשאי לשנות את הסגל.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> AddPlayerToTeamAsync(string sessionToken, int teamId, int playerId) =>
        InvokeRoleProtectedAsync(service => service.AddPlayerToTeam(sessionToken, teamId, playerId),
            message => new OperationResultDto { Message = message });

    /// <summary>מסיר שיוך מסגל עבור מאמן. השרת מחזיר 401 או 403 אם הבקשה חסומה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
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
