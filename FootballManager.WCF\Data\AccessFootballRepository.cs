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
            var searchPattern = $"%{searchText.Trim()}%";
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

    public void SeedDemoContent()
    {
        var teams = new[]
        {
            new TeamDto { Name = "Maccabi Netanya", Country = "Israel", League = "Ligat ha'Al", Budget = 1250000m },
            new TeamDto { Name = "Hapoel Haifa", Country = "Israel", League = "Ligat ha'Al", Budget = 980000m },
            new TeamDto { Name = "Bnei Sakhnin", Country = "Israel", League = "Ligat ha'Al", Budget = 760000m }
        };
        foreach (var team in teams)
        {
            if (!GetTeams().Any(existing => existing.Name == team.Name))
            {
                SaveTeam(team);
            }
        }

        var players = new[]
        {
            new PlayerDto { FirstName = "Noam", LastName = "Levi", Age = 24, Position = "Goalkeeper", Rating = 78, Speed = 62, Shooting = 30, Passing = 66, Defense = 80 },
            new PlayerDto { FirstName = "Omer", LastName = "Cohen", Age = 22, Position = "Defender", Rating = 76, Speed = 75, Shooting = 42, Passing = 70, Defense = 79 },
            new PlayerDto { FirstName = "Daniel", LastName = "Mizrahi", Age = 27, Position = "Defender", Rating = 81, Speed = 72, Shooting = 48, Passing = 76, Defense = 84 },
            new PlayerDto { FirstName = "Eitan", LastName = "Barak", Age = 21, Position = "Midfielder", Rating = 74, Speed = 77, Shooting = 65, Passing = 78, Defense = 66 },
            new PlayerDto { FirstName = "Ariel", LastName = "Peretz", Age = 25, Position = "Midfielder", Rating = 83, Speed = 82, Shooting = 79, Passing = 85, Defense = 70 },
            new PlayerDto { FirstName = "Yarden", LastName = "Shahar", Age = 23, Position = "Forward", Rating = 80, Speed = 87, Shooting = 82, Passing = 72, Defense = 40 },
            new PlayerDto { FirstName = "Itay", LastName = "Dahan", Age = 26, Position = "Forward", Rating = 77, Speed = 84, Shooting = 79, Passing = 68, Defense = 43 },
            new PlayerDto { FirstName = "Lior", LastName = "Avraham", Age = 20, Position = "Defender", Rating = 72, Speed = 74, Shooting = 39, Passing = 67, Defense = 75 },
            new PlayerDto { FirstName = "Matan", LastName = "Azulay", Age = 28, Position = "Goalkeeper", Rating = 79, Speed = 60, Shooting = 28, Passing = 69, Defense = 82 },
            new PlayerDto { FirstName = "Yuval", LastName = "Nissim", Age = 24, Position = "Midfielder", Rating = 75, Speed = 76, Shooting = 67, Passing = 77, Defense = 69 },
            new PlayerDto { FirstName = "Roi", LastName = "Sasson", Age = 22, Position = "Forward", Rating = 73, Speed = 85, Shooting = 76, Passing = 65, Defense = 39 },
            new PlayerDto { FirstName = "Gal", LastName = "Malka", Age = 27, Position = "Defender", Rating = 78, Speed = 70, Shooting = 44, Passing = 73, Defense = 81 }
        };
        var existingPlayers = GetPlayers(string.Empty, string.Empty, 0, 100).ToList();
        foreach (var player in players)
        {
            if (!existingPlayers.Any(existing => existing.FirstName == player.FirstName && existing.LastName == player.LastName))
            {
                SavePlayer(player);
                existingPlayers.Add(player);
            }
        }

        var allTeams = GetTeams();
        var allPlayers = GetPlayers(string.Empty, string.Empty, 0, 100);
        var assignments = new[]
        {
            (Team: "Maccabi Netanya", Players: new[] { "Noam Levi", "Omer Cohen", "Daniel Mizrahi", "Eitan Barak" }),
            (Team: "Hapoel Haifa", Players: new[] { "Ariel Peretz", "Yarden Shahar", "Itay Dahan", "Lior Avraham" }),
            (Team: "Bnei Sakhnin", Players: new[] { "Matan Azulay", "Yuval Nissim", "Roi Sasson", "Gal Malka" })
        };
        foreach (var assignment in assignments)
        {
            var team = allTeams.Single(existing => existing.Name == assignment.Team);
            var rosterIds = GetTeamRoster(team.TeamId).Select(player => player.PlayerId).ToHashSet();
            foreach (var fullName in assignment.Players)
            {
                var player = allPlayers.Single(existing => $"{existing.FirstName} {existing.LastName}" == fullName);
                if (!rosterIds.Contains(player.PlayerId))
                {
                    AddPlayerToTeam(team.TeamId, player.PlayerId);
                }
            }
        }
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
