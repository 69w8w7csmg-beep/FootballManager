using CoreWCF;
using FootballManager.Contracts;

namespace FootballManager.WCF.Contracts;

[ServiceContract(Name = "IFootballManagerService", Namespace = "urn:footballmanager:services")]
public interface IFootballManagerService
{
    [OperationContract(Name = "GetServiceStatus")]
    ServiceStatusDto GetServiceStatus();

    [OperationContract(Name = "Register")]
    OperationResultDto Register(RegisterRequestDto request);

    [OperationContract(Name = "Login")]
    LoginResponseDto Login(LoginRequestDto request);

    [OperationContract(Name = "Logout")]
    OperationResultDto Logout(string sessionToken);

    [OperationContract(Name = "GetManagedUsers")]
    ManagedUsersResponseDto GetManagedUsers(string sessionToken);

    [OperationContract(Name = "CreateManagedUser")]
    OperationResultDto CreateManagedUser(string sessionToken, CreateManagedUserRequestDto request);

    [OperationContract(Name = "DeleteManagedUser")]
    OperationResultDto DeleteManagedUser(string sessionToken, int userId);

    [OperationContract(Name = "UpdateUserRole")]
    OperationResultDto UpdateUserRole(string sessionToken, int userId, string role);

    [OperationContract(Name = "SetUserBlocked")]
    OperationResultDto SetUserBlocked(string sessionToken, int userId, bool isBlocked);

    [OperationContract(Name = "GetPlayers")]
    PlayersResponseDto GetPlayers(string searchText, string position, int minimumRating, int maximumRating);

    [OperationContract(Name = "GetTeams")]
    TeamsResponseDto GetTeams();

    [OperationContract(Name = "GetTeamRoster")]
    PlayersResponseDto GetTeamRoster(int teamId);

    [OperationContract(Name = "GetMyFavorites")]
    PlayersResponseDto GetMyFavorites(string sessionToken);

    [OperationContract(Name = "AddFavorite")]
    OperationResultDto AddFavorite(string sessionToken, int playerId);

    [OperationContract(Name = "RemoveFavorite")]
    OperationResultDto RemoveFavorite(string sessionToken, int playerId);

    [OperationContract(Name = "SavePlayer")]
    OperationResultDto SavePlayer(string sessionToken, PlayerDto player);

    [OperationContract(Name = "DeletePlayer")]
    OperationResultDto DeletePlayer(string sessionToken, int playerId);

    [OperationContract(Name = "SaveTeam")]
    OperationResultDto SaveTeam(string sessionToken, TeamDto team);

    [OperationContract(Name = "DeleteTeam")]
    OperationResultDto DeleteTeam(string sessionToken, int teamId);

    [OperationContract(Name = "AddPlayerToTeam")]
    OperationResultDto AddPlayerToTeam(string sessionToken, int teamId, int playerId);

    [OperationContract(Name = "RemovePlayerFromTeam")]
    OperationResultDto RemovePlayerFromTeam(string sessionToken, int teamId, int playerId);
}
