// שליפה, שמירה ומחיקה של משחקים בטבלת Matches במסד Access.
using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

public sealed class AccessMatchRepository(AccessDatabase database)
{
    public List<MatchDto> GetMatches(string? searchText, DateTime? matchDate)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            conditions.Add("(InStr(1, H.[Name], ?, 1) > 0 OR InStr(1, A.[Name], ?, 1) > 0)");
            string teamName = searchText.Trim();
            command.Parameters.AddWithValue("@HomeSearch", teamName);
            command.Parameters.AddWithValue("@AwaySearch", teamName);
        }

        if (matchDate.HasValue)
        {
            conditions.Add("DateValue(M.[MatchDate]) = ?");
            command.Parameters.Add("@MatchDate", OleDbType.Date).Value = matchDate.Value.Date;
        }

        command.CommandText = "SELECT M.[MatchId], M.[HomeTeamId], M.[AwayTeamId], M.[MatchDate], " +
            "M.[HomeScore], M.[AwayScore], H.[Name], A.[Name] FROM " +
            "([Matches] AS M INNER JOIN [Teams] AS H ON M.[HomeTeamId] = H.[TeamId]) " +
            "INNER JOIN [Teams] AS A ON M.[AwayTeamId] = A.[TeamId]" +
            (conditions.Count == 0 ? "" : " WHERE " + string.Join(" AND ", conditions)) +
            " ORDER BY M.[MatchDate] DESC, M.[MatchId] DESC";

        using var reader = command.ExecuteReader();
        var matches = new List<MatchDto>();
        while (reader.Read())
        {
            matches.Add(new MatchDto
            {
                MatchId = reader.GetInt32(0), HomeTeamId = reader.GetInt32(1), AwayTeamId = reader.GetInt32(2),
                MatchDate = reader.GetDateTime(3), HomeScore = Convert.ToInt32(reader[4]),
                AwayScore = Convert.ToInt32(reader[5]), HomeTeamName = reader.GetString(6), AwayTeamName = reader.GetString(7)
            });
        }
        return matches;
    }

    public bool Save(MatchDto match)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = match.MatchId == 0
            ? "INSERT INTO [Matches] ([HomeTeamId], [AwayTeamId], [MatchDate], [HomeScore], [AwayScore]) VALUES (?, ?, ?, ?, ?)"
            : "UPDATE [Matches] SET [HomeTeamId] = ?, [AwayTeamId] = ?, [MatchDate] = ?, [HomeScore] = ?, [AwayScore] = ? WHERE [MatchId] = ?";
        command.Parameters.AddWithValue("@HomeTeamId", match.HomeTeamId);
        command.Parameters.AddWithValue("@AwayTeamId", match.AwayTeamId);
        command.Parameters.Add("@MatchDate", OleDbType.Date).Value = match.MatchDate;
        command.Parameters.AddWithValue("@HomeScore", match.HomeScore);
        command.Parameters.AddWithValue("@AwayScore", match.AwayScore);
        if (match.MatchId != 0)
        {
            command.Parameters.AddWithValue("@MatchId", match.MatchId);
        }
        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(int matchId)
    {
        using var connection = database.OpenConnection();
        using var command = new OleDbCommand("DELETE FROM [Matches] WHERE [MatchId] = ?", connection);
        command.Parameters.AddWithValue("@MatchId", matchId);
        return command.ExecuteNonQuery() == 1;
    }
}
