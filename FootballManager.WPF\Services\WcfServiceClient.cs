using System.ServiceModel;
using FootballManager.Contracts;

namespace FootballManager.WPF.Services;

public sealed class WcfServiceClient
{
    private const string ServiceAddress = "http://localhost:5152/FootballManagerService";

    public Task<ServiceStatusDto> GetServiceStatusAsync() =>
        Task.Run(() => Invoke(service => service.GetServiceStatus()));

    public Task<OperationResultDto> RegisterAsync(RegisterRequestDto request) =>
        Task.Run(() => Invoke(service => service.Register(request)));

    public Task<LoginResponseDto> LoginAsync(LoginRequestDto request) =>
        Task.Run(() => Invoke(service => service.Login(request)));

    public Task<OperationResultDto> LogoutAsync(string sessionToken) =>
        Task.Run(() => Invoke(service => service.Logout(sessionToken)));

    public Task<ManagedUsersResponseDto> GetManagedUsersAsync(string sessionToken) =>
        Task.Run(() => Invoke(service => service.GetManagedUsers(sessionToken)));

    public Task<OperationResultDto> CreateManagedUserAsync(
        string sessionToken,
        CreateManagedUserRequestDto request) =>
        Task.Run(() => Invoke(service => service.CreateManagedUser(sessionToken, request)));

    public Task<OperationResultDto> DeleteManagedUserAsync(string sessionToken, int userId) =>
        Task.Run(() => Invoke(service => service.DeleteManagedUser(sessionToken, userId)));

    public Task<OperationResultDto> UpdateUserRoleAsync(string sessionToken, int userId, string role) =>
        Task.Run(() => Invoke(service => service.UpdateUserRole(sessionToken, userId, role)));

    public Task<OperationResultDto> SetUserBlockedAsync(string sessionToken, int userId, bool isBlocked) =>
        Task.Run(() => Invoke(service => service.SetUserBlocked(sessionToken, userId, isBlocked)));

    public Task<PlayersResponseDto> GetPlayersAsync(
        string searchText = "",
        string position = "",
        int minimumRating = 0,
        int maximumRating = 100) =>
        Task.Run(() => Invoke(service => service.GetPlayers(searchText, position, minimumRating, maximumRating)));

    public Task<TeamsResponseDto> GetTeamsAsync() =>
        Task.Run(() => Invoke(service => service.GetTeams()));

    public Task<PlayersResponseDto> GetTeamRosterAsync(int teamId) =>
        Task.Run(() => Invoke(service => service.GetTeamRoster(teamId)));

    public Task<PlayersResponseDto> GetMyFavoritesAsync(string sessionToken) =>
        Task.Run(() => Invoke(service => service.GetMyFavorites(sessionToken)));

    public Task<OperationResultDto> AddFavoriteAsync(string sessionToken, int playerId) =>
        Task.Run(() => Invoke(service => service.AddFavorite(sessionToken, playerId)));

    public Task<OperationResultDto> RemoveFavoriteAsync(string sessionToken, int playerId) =>
        Task.Run(() => Invoke(service => service.RemoveFavorite(sessionToken, playerId)));

    public Task<OperationResultDto> SavePlayerAsync(string sessionToken, PlayerDto player) =>
        Task.Run(() => Invoke(service => service.SavePlayer(sessionToken, player)));

    public Task<OperationResultDto> DeletePlayerAsync(string sessionToken, int playerId) =>
        Task.Run(() => Invoke(service => service.DeletePlayer(sessionToken, playerId)));

    public Task<OperationResultDto> SaveTeamAsync(string sessionToken, TeamDto team) =>
        Task.Run(() => Invoke(service => service.SaveTeam(sessionToken, team)));

    public Task<OperationResultDto> DeleteTeamAsync(string sessionToken, int teamId) =>
        Task.Run(() => Invoke(service => service.DeleteTeam(sessionToken, teamId)));

    public Task<OperationResultDto> AddPlayerToTeamAsync(string sessionToken, int teamId, int playerId) =>
        Task.Run(() => Invoke(service => service.AddPlayerToTeam(sessionToken, teamId, playerId)));

    public Task<OperationResultDto> RemovePlayerFromTeamAsync(string sessionToken, int teamId, int playerId) =>
        Task.Run(() => Invoke(service => service.RemovePlayerFromTeam(sessionToken, teamId, playerId)));

    private static T Invoke<T>(Func<IFootballManagerService, T> operation)
    {
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = 1024 * 1024
        };
        var address = new EndpointAddress(ServiceAddress);
        var factory = new ChannelFactory<IFootballManagerService>(binding, address);
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
