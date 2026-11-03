using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

public sealed class AccessFootballRepository
{
    private readonly AccessDatabase _database;

    public AccessFootballRepository(AccessDatabase database)
    {
        _database = database;
    }

    public IReadOnlyList<PlayerDto> GetPlayers(string searchText, string position, int minimumRating, int maximumRating)
    {
        var conditions = new List<string> { "[Rating] BETWEEN ? AND ?" };
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand();
        command.Connection = connection;
        command.Parameters.AddWithValue("@MinimumRating", minimumRating);
        command.Parameters.AddWithValue("@MaximumRating", maximumRating);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            conditions.Add("([FirstName] LIKE ? OR [LastName] LIKE ?)");
            var searchPattern = $"*{searchText.Trim()}*";
            command.Parameters.AddWithValue("@FirstNameSearch", searchPattern);
            command.Parameters.AddWithValue("@LastNameSearch", searchPattern);
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            conditions.Add("[Position] = ?");
            command.Parameters.AddWithValue("@Position", position.Trim());
        }

        command.CommandText =
            "SELECT [PlayerId], [FirstName], [LastName], [Age], [Position], [Rating], " +
            "[Speed], [Shooting], [Passing], [Defense] FROM [Players] WHERE " +
            string.Join(" AND ", conditions) + " ORDER BY [LastName], [FirstName]";

        using var reader = command.ExecuteReader();
        var players = new List<PlayerDto>();
        while (reader.Read())
        {
            players.Add(ReadPlayer(reader));
        }

        return players;
    }

    public IReadOnlyList<TeamDto> GetTeams()
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT [TeamId], [Name], [Country], [League], [Budget] FROM [Teams] ORDER BY [Name]",
            connection);
        using var reader = command.ExecuteReader();
        var teams = new List<TeamDto>();
        while (reader.Read())
        {
            teams.Add(ReadTeam(reader));
        }

        return teams;
    }

    public IReadOnlyList<PlayerDto> GetTeamRoster(int teamId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT P.[PlayerId], P.[FirstName], P.[LastName], P.[Age], P.[Position], P.[Rating], " +
            "P.[Speed], P.[Shooting], P.[Passing], P.[Defense] " +
            "FROM [Players] AS P INNER JOIN [TeamPlayers] AS TP ON P.[PlayerId] = TP.[PlayerId] " +
            "WHERE TP.[TeamId] = ? AND TP.[EndDate] IS NULL ORDER BY P.[Position], P.[LastName]",
            connection);
        command.Parameters.AddWithValue("@TeamId", teamId);
        using var reader = command.ExecuteReader();
        var players = new List<PlayerDto>();
        while (reader.Read())
        {
            players.Add(ReadPlayer(reader));
        }

        return players;
    }

    public IReadOnlyList<PlayerDto> GetFavorites(int userId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT P.[PlayerId], P.[FirstName], P.[LastName], P.[Age], P.[Position], P.[Rating], " +
            "P.[Speed], P.[Shooting], P.[Passing], P.[Defense] " +
            "FROM [Players] AS P INNER JOIN [Favorites] AS F ON P.[PlayerId] = F.[PlayerId] " +
            "WHERE F.[UserId] = ? ORDER BY P.[LastName], P.[FirstName]",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        using var reader = command.ExecuteReader();
        var players = new List<PlayerDto>();
        while (reader.Read())
        {
            players.Add(ReadPlayer(reader));
        }

        return players;
    }

    public bool SavePlayer(PlayerDto player)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand { Connection = connection };
        if (player.PlayerId == 0)
        {
            command.CommandText =
                "INSERT INTO [Players] ([FirstName], [LastName], [Age], [Position], [Rating], [Speed], [Shooting], [Passing], [Defense]) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
        }
        else
        {
            command.CommandText =
                "UPDATE [Players] SET [FirstName] = ?, [LastName] = ?, [Age] = ?, [Position] = ?, [Rating] = ?, " +
                "[Speed] = ?, [Shooting] = ?, [Passing] = ?, [Defense] = ? WHERE [PlayerId] = ?";
        }

        AddPlayerValues(command, player);
        if (player.PlayerId != 0)
        {
            command.Parameters.AddWithValue("@PlayerId", player.PlayerId);
        }

        return command.ExecuteNonQuery() == 1;
    }

    public bool DeletePlayer(int playerId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand("DELETE FROM [Players] WHERE [PlayerId] = ?", connection);
        command.Parameters.AddWithValue("@PlayerId", playerId);
        return command.ExecuteNonQuery() == 1;
    }

    public bool SaveTeam(TeamDto team)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand { Connection = connection };
        if (team.TeamId == 0)
        {
            command.CommandText =
                "INSERT INTO [Teams] ([Name], [Country], [League], [Budget]) VALUES (?, ?, ?, ?)";
        }
        else
        {
            command.CommandText =
                "UPDATE [Teams] SET [Name] = ?, [Country] = ?, [League] = ?, [Budget] = ? WHERE [TeamId] = ?";
        }

        command.Parameters.AddWithValue("@Name", team.Name);
        command.Parameters.AddWithValue("@Country", team.Country);
        command.Parameters.AddWithValue("@League", team.League);
        command.Parameters.AddWithValue("@Budget", team.Budget);
        if (team.TeamId != 0)
        {
            command.Parameters.AddWithValue("@TeamId", team.TeamId);
        }

        return command.ExecuteNonQuery() == 1;
    }

    public bool DeleteTeam(int teamId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand("DELETE FROM [Teams] WHERE [TeamId] = ?", connection);
        command.Parameters.AddWithValue("@TeamId", teamId);
        return command.ExecuteNonQuery() == 1;
    }

    public void AddFavorite(int userId, int playerId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "INSERT INTO [Favorites] ([UserId], [PlayerId]) VALUES (?, ?)",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@PlayerId", playerId);
        command.ExecuteNonQuery();
    }

    public bool RemoveFavorite(int userId, int playerId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "DELETE FROM [Favorites] WHERE [UserId] = ? AND [PlayerId] = ?",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@PlayerId", playerId);
        return command.ExecuteNonQuery() == 1;
    }

    public void AddPlayerToTeam(int teamId, int playerId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "INSERT INTO [TeamPlayers] ([TeamId], [PlayerId], [StartDate], [EndDate]) VALUES (?, ?, ?, NULL)",
            connection);
        command.Parameters.AddWithValue("@TeamId", teamId);
        command.Parameters.AddWithValue("@PlayerId", playerId);
        command.Parameters.AddWithValue("@StartDate", DateTime.Today);
        command.ExecuteNonQuery();
    }

    public bool RemovePlayerFromTeam(int teamId, int playerId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "DELETE FROM [TeamPlayers] WHERE [TeamId] = ? AND [PlayerId] = ?",
            connection);
        command.Parameters.AddWithValue("@TeamId", teamId);
        command.Parameters.AddWithValue("@PlayerId", playerId);
        return command.ExecuteNonQuery() == 1;
    }

    private static void AddPlayerValues(OleDbCommand command, PlayerDto player)
    {
        command.Parameters.AddWithValue("@FirstName", player.FirstName);
        command.Parameters.AddWithValue("@LastName", player.LastName);
        command.Parameters.AddWithValue("@Age", player.Age);
        command.Parameters.AddWithValue("@Position", player.Position);
        command.Parameters.AddWithValue("@Rating", player.Rating);
        command.Parameters.AddWithValue("@Speed", player.Speed);
        command.Parameters.AddWithValue("@Shooting", player.Shooting);
        command.Parameters.AddWithValue("@Passing", player.Passing);
        command.Parameters.AddWithValue("@Defense", player.Defense);
    }

    private static PlayerDto ReadPlayer(OleDbDataReader reader) => new()
    {
        PlayerId = Convert.ToInt32(reader["PlayerId"]),
        FirstName = Convert.ToString(reader["FirstName"]) ?? string.Empty,
        LastName = Convert.ToString(reader["LastName"]) ?? string.Empty,
        Age = Convert.ToInt32(reader["Age"]),
        Position = Convert.ToString(reader["Position"]) ?? string.Empty,
        Rating = Convert.ToInt32(reader["Rating"]),
        Speed = Convert.ToInt32(reader["Speed"]),
        Shooting = Convert.ToInt32(reader["Shooting"]),
        Passing = Convert.ToInt32(reader["Passing"]),
        Defense = Convert.ToInt32(reader["Defense"])
    };

    private static TeamDto ReadTeam(OleDbDataReader reader) => new()
    {
        TeamId = Convert.ToInt32(reader["TeamId"]),
        Name = Convert.ToString(reader["Name"]) ?? string.Empty,
        Country = Convert.ToString(reader["Country"]) ?? string.Empty,
        League = Convert.ToString(reader["League"]) ?? string.Empty,
        Budget = Convert.ToDecimal(reader["Budget"])
    };
}
