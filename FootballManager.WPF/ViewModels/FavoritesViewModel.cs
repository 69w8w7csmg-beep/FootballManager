// תפקיד הקובץ: טוען ומעדכן את המועדפים של המשתמש.
// שכבה: ViewModel של לקוח WPF.
// משתמשים: מסכי השחקנים והמועדפים בחלון הראשי.
// קשר בפרויקט: שולח בקשות דרך WcfServiceClient ומחזיר תוצאות לתצוגה.
using System.ServiceModel;
using FootballManager.Contracts;
using FootballManager.WPF.Services;

namespace FootballManager.WPF.ViewModels;

public sealed class FavoritesViewModel(WcfServiceClient service)
{
    public Task<PlayersResponseDto> LoadAsync(string sessionToken) =>
        ThroughServiceAsync(() => service.GetMyFavoritesAsync(sessionToken),
            () => new PlayersResponseDto { Message = "לא ניתן לטעון מועדפים. בדוק שהשרת פועל." });

    public Task<OperationResultDto> AddAsync(string sessionToken, PlayerDto player) =>
        ThroughServiceAsync(() => service.AddFavoriteAsync(sessionToken, player.PlayerId),
            () => new OperationResultDto { Message = "לא ניתן להוסיף מועדף. בדוק שהשרת פועל." });

    public Task<OperationResultDto> RemoveAsync(string sessionToken, PlayerDto player) =>
        ThroughServiceAsync(() => service.RemoveFavoriteAsync(sessionToken, player.PlayerId),
            () => new OperationResultDto { Message = "לא ניתן להסיר מועדף. בדוק שהשרת פועל." });

    private static async Task<T> ThroughServiceAsync<T>(Func<Task<T>> action, Func<T> onFailure)
    {
        try { return await action(); }
        catch (Exception error) when (error is CommunicationException or TimeoutException)
        {
            return onFailure();
        }
    }
}
