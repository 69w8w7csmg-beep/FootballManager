// מטרה: מגדיר את פעולות חוזה SOAP של השרת.
// מי משתמש: FootballManagerService, WPF, Android ומתאם האתר.
// פעולות: חשבון, תוכן, משחקים, העברות, סטטיסטיקות ובינה.
// תלויות: CoreWCF ו־DTO משותפים.
using CoreWCF;
using FootballManager.Contracts;

namespace FootballManager.WCF.Contracts;

/// <summary>חוזה WCF שמפרט את פעולות החשבון, התוכן, המשחקים ו־C4. WPF משתמש בחתימות כדי לקרוא לשירות; FootballManagerService מממש אותן בשרת.</summary>
[ServiceContract(Name = "IFootballManagerService", Namespace = "urn:footballmanager:services")]
public interface IFootballManagerService
{
    /// <summary>קורא את ההעברות למאמן. בלי אסימון פעיל השרת מחזיר 401, ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת העברות כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetTransfers")]
    TransfersResponseDto GetTransfers(string sessionToken);

    /// <summary>מוסיף העברה אם המזהה 0, או מעדכן העברה קיימת. מיועד למאמן. דחיית התחברות מחזירה 401 ודחיית הרשאה מחזירה 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "SaveTransfer")]
    OperationResultDto SaveTransfer(string sessionToken, TransferDto transfer);

    /// <summary>מוחק העברה קיימת לפי מזהה. השרת מתיר למאמן בלבד ומחזיר 401 או 403 בדחייה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="transferId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "DeleteTransfer")]
    OperationResultDto DeleteTransfer(string sessionToken, int transferId);

    /// <summary>קורא את סטטיסטיקות סבב 4 למאמן. דחייה מחזירה 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת סטטיסטיקות כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetPlayerStatistics")]
    PlayerStatisticsResponseDto GetPlayerStatistics(string sessionToken);

    /// <summary>שומר סטטיסטיקה למשחק לפי מזהה. השרת בודק מאמן, ומחזיר 401 או 403 אם הבקשה חסומה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statistic">נתוני הסטטיסטיקה ששייכים לשחקן ולמשחק.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "SavePlayerStatistic")]
    OperationResultDto SavePlayerStatistic(string sessionToken, PlayerStatisticDto statistic);

    /// <summary>מוחק סטטיסטיקה לפי מזהה עבור מאמן. דחייה מחזירה 401 או 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="statisticId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "DeletePlayerStatistic")]
    OperationResultDto DeletePlayerStatistic(string sessionToken, int statisticId);

    /// <summary>קורא משחקים לפי חיפוש ותאריך. שחקן ומאמן יכולים לקרוא. אורח מקבל 401 ואדמין מקבל 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="searchText">שם או חלק משם לחיפוש.</param>
    /// <param name="matchDate">תאריך לסינון משחקים, או null לכל התאריכים.</param>
    /// <returns>תוצאת שליפה ורשימת משחקים כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetMatches")]
    MatchesResponseDto GetMatches(string sessionToken, string searchText, DateTime? matchDate);

    /// <summary>שומר תאריך, קבוצות ותוצאה עבור מאמן. השרת מחזיר 401 ללא כניסה ו־403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="match">נתוני משחק, כולל תאריך, מזהי הקבוצות ושערים.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "SaveMatch")]
    OperationResultDto SaveMatch(string sessionToken, MatchDto match);

    /// <summary>מוחק משחק לפי מזהה עבור מאמן. השרת מחזיר 401 בלי כניסה ו־403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="matchId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "DeleteMatch")]
    OperationResultDto DeleteMatch(string sessionToken, int matchId);

    /// <summary>בודק אם השרת רץ ומסד Access מוכן לשימוש. זמין גם לאורח.</summary>
    /// <returns>שם היישום, מצב המסד והודעת השרת.</returns>
    [OperationContract(Name = "GetServiceStatus")]
    ServiceStatusDto GetServiceStatus();

    /// <summary>פותח חשבון ציבורי מסוג Player לאחר בדיקת השם והסיסמה. השירות מחזיר הודעה על הצלחה או על קלט לא תקין.</summary>
    /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "Register")]
    OperationResultDto Register(RegisterRequestDto request);

    /// <summary>מטפלת בהזדהות: שולחת פרטי כניסה לבדיקה בשירות ומחזירה את תוצאת ההתחברות. נקראת בקוד מתוך Map Football Manager Web Api, Login Async.</summary>
    /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
    /// <returns>תוצאה ובה אסימון, שם ותפקיד בהצלחה, או הודעה על שגיאה.</returns>
    [OperationContract(Name = "Login")]
    LoginResponseDto Login(LoginRequestDto request);

    /// <summary>מסיימת את ההפעלה המחוברת ומנקה את מצב המשתמש בצד הלקוח. נקראת בקוד מתוך Map Football Manager Web Api, Logout Async.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "Logout")]
    OperationResultDto Logout(string sessionToken);

    /// <summary>קורא את רשימת המשתמשים עבור Admin. בלי התחברות השרת מחזיר 401 ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת משתמשים בלי סיסמאות.</returns>
    [OperationContract(Name = "GetManagedUsers")]
    ManagedUsersResponseDto GetManagedUsers(string sessionToken);

    /// <summary>יוצר חשבון בתפקיד שבחר Admin. הסיסמה מגובבת בשרת. חסימה מחזירה 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "CreateManagedUser")]
    OperationResultDto CreateManagedUser(string sessionToken, CreateManagedUserRequestDto request);

    /// <summary>מוחק חשבון לפי מזהה עבור Admin. השרת שומר על האדמין הפעיל האחרון ומחזיר 401 או 403 בדחיית גישה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "DeleteManagedUser")]
    OperationResultDto DeleteManagedUser(string sessionToken, int userId);

    /// <summary>משנה תפקיד של חשבון עבור Admin. השרת בודק את התפקיד ואת מצב האדמין האחרון. גישה שנדחית מחזירה 401 או 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "UpdateUserRole")]
    OperationResultDto UpdateUserRole(string sessionToken, int userId, string role);

    /// <summary>מעדכן חסימה עבור Admin. ללא אסימון מתקבל 401 ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="isBlocked">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "SetUserBlocked")]
    OperationResultDto SetUserBlocked(string sessionToken, int userId, bool isBlocked);

    /// <summary>קורא את קטלוג השחקנים הציבורי לפי שם, עמדה וטווח דירוג.</summary>
    /// <param name="searchText">שם או חלק משם לחיפוש.</param>
    /// <param name="position">עמדה לסינון, או מחרוזת ריקה לכל העמדות.</param>
    /// <param name="minimumRating">הדירוג הנמוך ביותר במסנן.</param>
    /// <param name="maximumRating">הדירוג הגבוה ביותר במסנן.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetPlayers")]
    PlayersResponseDto GetPlayers(string searchText, string position, int minimumRating, int maximumRating);

    /// <summary>מבקשת מהשרת ניתוח AI לשחקן לפי מזהה. השרת מזהה את החשבון מתוך האסימון ומרשה את הפעולה ל־Player בלבד.</summary>
    /// <param name="sessionToken">אסימון ההתחברות שהתקבל מהשרת.</param>
    /// <param name="playerId">מזהה השחקן הציבורי לניתוח.</param>
    /// <returns>תוצאת הניתוח או הודעת כשל בטוחה להצגה.</returns>
    [OperationContract(Name = "AnalyzePlayer")]
    Task<PlayerAnalysisResponseDto> AnalyzePlayerAsync(string sessionToken, int playerId);

    /// <summary>שולחת שאלה או איפוס צ'אט לשרת; השרת מאמת Session ותפקיד ומחזיק היסטוריה למשתמש.</summary>
    [OperationContract(Name = "Chat")]
    Task<AiChatResponseDto> ChatAsync(AiChatRequestDto request);

    /// <summary>קורא את הקבוצות הציבוריות מ־Access דרך השירות.</summary>
    /// <returns>תוצאת שליפה ורשימת קבוצות כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetTeams")]
    TeamsResponseDto GetTeams();

    /// <summary>קורא את השחקנים המשויכים לקבוצה לפי מזהה קבוצה.</summary>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetTeamRoster")]
    PlayersResponseDto GetTeamRoster(int teamId);

    /// <summary>קורא רק את המועדפים של Player שמזוהה באסימון. בלי התחברות מתקבל 401 ולתפקיד אחר 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    [OperationContract(Name = "GetMyFavorites")]
    PlayersResponseDto GetMyFavorites(string sessionToken);

    /// <summary>מוסיף שחקן למועדפים של Player שמזוהה באסימון. אין קלט של מזהה הבעלים. השרת מחזיר 401 או 403 בדחייה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "AddFavorite")]
    OperationResultDto AddFavorite(string sessionToken, int playerId);

    /// <summary>מסיר שחקן מרשימת המועדפים של Player שמזוהה באסימון. גישה שנדחית מחזירה 401 או 403.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "RemoveFavorite")]
    OperationResultDto RemoveFavorite(string sessionToken, int playerId);

    /// <summary>מוסיף או מעדכן רשומת שחקן עבור מאמן. השרת מחזיר 401 ללא התחברות או 403 ללא הרשאה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="player">נתוני השחקן או מזהה השחקן.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "SavePlayer")]
    OperationResultDto SavePlayer(string sessionToken, PlayerDto player);

    /// <summary>מוחק שחקן לפי מזהה עבור מאמן. השרת מחזיר 401 או 403 בדחיית גישה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "DeletePlayer")]
    OperationResultDto DeletePlayer(string sessionToken, int playerId);

    /// <summary>מוסיף או מעדכן קבוצה עבור מאמן. השרת מחזיר 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="team">נתוני הקבוצה או מזהה הקבוצה.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "SaveTeam")]
    OperationResultDto SaveTeam(string sessionToken, TeamDto team);

    /// <summary>מוחק קבוצה לפי מזהה עבור מאמן. השרת מחזיר 401 בלי כניסה או 403 לתפקיד אחר.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "DeleteTeam")]
    OperationResultDto DeleteTeam(string sessionToken, int teamId);

    /// <summary>משייך שחקן לסגל עבור מאמן. השרת מחזיר 401 או 403 כשהחשבון אינו רשאי לשנות את הסגל.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "AddPlayerToTeam")]
    OperationResultDto AddPlayerToTeam(string sessionToken, int teamId, int playerId);

    /// <summary>מסיר שיוך מסגל עבור מאמן. השרת מחזיר 401 או 403 אם הבקשה חסומה.</summary>
    /// <param name="sessionToken">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
    /// <param name="teamId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="playerId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    [OperationContract(Name = "RemovePlayerFromTeam")]
    OperationResultDto RemovePlayerFromTeam(string sessionToken, int teamId, int playerId);
}
