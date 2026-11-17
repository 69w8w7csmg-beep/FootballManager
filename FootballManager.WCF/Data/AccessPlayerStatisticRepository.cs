// קריאה, שמירה ומחיקה של סטטיסטיקות משחק בטבלת PlayerStatistics הקיימת.
using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

public sealed class AccessPlayerStatisticRepository(AccessDatabase database)
{
    public List<PlayerStatisticDto> GetStatistics()
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT S.[StatisticId], S.[PlayerId], S.[MatchId], S.[Goals], S.[Assists], " +
            "S.[MinutesPlayed], S.[Rating], P.[FirstName], P.[LastName], M.[MatchDate], H.[Name], A.[Name] " +
            "FROM ((([PlayerStatistics] AS S INNER JOIN [Players] AS P ON S.[PlayerId] = P.[PlayerId]) " +
            "INNER JOIN [Matches] AS M ON S.[MatchId] = M.[MatchId]) " +
            "INNER JOIN [Teams] AS H ON M.[HomeTeamId] = H.[TeamId]) " +
            "INNER JOIN [Teams] AS A ON M.[AwayTeamId] = A.[TeamId] " +
            "ORDER BY M.[MatchDate] DESC, S.[StatisticId] DESC";

        using var reader = command.ExecuteReader();
        var statistics = new List<PlayerStatisticDto>();
        while (reader.Read())
        {
            statistics.Add(new PlayerStatisticDto
            {
                StatisticId = reader.GetInt32(0),
                PlayerId = reader.GetInt32(1),
                MatchId = reader.GetInt32(2),
                Goals = Convert.ToInt32(reader[3]),
                Assists = Convert.ToInt32(reader[4]),
                MinutesPlayed = Convert.ToInt32(reader[5]),
                Rating = Convert.ToSingle(reader[6]),
                PlayerName = $"{reader.GetString(7)} {reader.GetString(8)}",
                MatchLabel = $"{reader.GetString(10)} מול {reader.GetString(11)} · {reader.GetDateTime(9):dd.MM.yyyy}"
            });
        }

        return statistics;
    }

    public bool Save(PlayerStatisticDto statistic)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = statistic.StatisticId == 0
            ? "INSERT INTO [PlayerStatistics] " +
              "([PlayerId], [MatchId], [Goals], [Assists], [MinutesPlayed], [Rating]) " +
              "VALUES (?, ?, ?, ?, ?, ?)"
            : "UPDATE [PlayerStatistics] SET [PlayerId] = ?, [MatchId] = ?, [Goals] = ?, " +
              "[Assists] = ?, [MinutesPlayed] = ?, [Rating] = ? WHERE [StatisticId] = ?";
        command.Parameters.Add("@PlayerId", OleDbType.Integer).Value = statistic.PlayerId;
        command.Parameters.Add("@MatchId", OleDbType.Integer).Value = statistic.MatchId;
        command.Parameters.Add("@Goals", OleDbType.SmallInt).Value = statistic.Goals;
        command.Parameters.Add("@Assists", OleDbType.SmallInt).Value = statistic.Assists;
        command.Parameters.Add("@MinutesPlayed", OleDbType.SmallInt).Value = statistic.MinutesPlayed;
        command.Parameters.Add("@Rating", OleDbType.Single).Value = statistic.Rating;
        if (statistic.StatisticId != 0)
        {
            command.Parameters.Add("@StatisticId", OleDbType.Integer).Value = statistic.StatisticId;
        }

        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(int statisticId)
    {
        using var connection = database.OpenConnection();
        using var command = new OleDbCommand(
            "DELETE FROM [PlayerStatistics] WHERE [StatisticId] = ?", connection);
        command.Parameters.Add("@StatisticId", OleDbType.Integer).Value = statisticId;
        return command.ExecuteNonQuery() == 1;
    }
}
