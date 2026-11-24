// מטרה: מנהל פעולות מועדפים עבור מסך WPF.
// מי משתמש: MainWindow.
// פעולות: LoadAsync, AddAsync ו־RemoveAsync.
// תלויות: WcfServiceClient ואסימון מהחשבון.
using System.ServiceModel;
using FootballManager.Contracts;
using FootballManager.WPF.Services;

namespace FootballManager.WPF.ViewModels;

public sealed class FavoritesViewModel(WcfServiceClient service)
{
    /// <summary>שולף את רשימת המועדפים של חשבון Player ומחזיר את רשימת השחקנים או הודעת שגיאה.</summary>
    /// <param name="sessionToken">אסימון שהשרת מנפיק בכניסה ומשתמש בו כדי לזהות את החשבון.</param>
    /// <returns>תוצאת שליפה ורשימת שחקנים כאשר הבקשה הצליחה.</returns>
    public Task<PlayersResponseDto> LoadAsync(string sessionToken) =>
        ThroughServiceAsync(() => service.GetMyFavoritesAsync(sessionToken),
            () => new PlayersResponseDto { Message = "לא ניתן לטעון מועדפים. בדוק שהשרת פועל." });

    /// <summary>מבקש מ־WCF להוסיף את השחקן למועדפים של החשבון באסימון, ומחזיר תוצאת פעולה.</summary>
    /// <param name="sessionToken">אסימון שהשרת מנפיק בכניסה ומשתמש בו כדי לזהות את החשבון.</param>
    /// <param name="player">רשומת השחקן שנבחרה או נערכת.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> AddAsync(string sessionToken, PlayerDto player) =>
        ThroughServiceAsync(() => service.AddFavoriteAsync(sessionToken, player.PlayerId),
            () => new OperationResultDto { Message = "לא ניתן להוסיף מועדף. בדוק שהשרת פועל." });

    /// <summary>מבקש להסיר את השחקן מהמועדפים של החשבון באסימון, ומחזיר תוצאת פעולה.</summary>
    /// <param name="sessionToken">אסימון שהשרת מנפיק בכניסה ומשתמש בו כדי לזהות את החשבון.</param>
    /// <param name="player">רשומת השחקן שנבחרה או נערכת.</param>
    /// <returns>תוצאת הצלחה והודעה להצגה.</returns>
    public Task<OperationResultDto> RemoveAsync(string sessionToken, PlayerDto player) =>
        ThroughServiceAsync(() => service.RemoveFavoriteAsync(sessionToken, player.PlayerId),
            () => new OperationResultDto { Message = "לא ניתן להסיר מועדף. בדוק שהשרת פועל." });

    /// <summary>מחזיר את תשובת הקריאה, או בונה הודעה חלופית בכשל תקשורת או בזמן המתנה.</summary>
    /// <param name="action">הפעולה שמפעילים כשהמשתמש לוחץ.</param>
    /// <param name="onFailure">פונקציה שמחזירה תוצאה חלופית לאחר כשל תקשורת.</param>
    private static async Task<T> ThroughServiceAsync<T>(Func<Task<T>> action, Func<T> onFailure)
    {
        try { return await action(); }
        catch (Exception error) when (error is CommunicationException or TimeoutException)
        {
            return onFailure();
        }
    }
}
