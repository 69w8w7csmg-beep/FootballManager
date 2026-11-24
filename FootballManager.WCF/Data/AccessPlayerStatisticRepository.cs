// מטרה: קורא ושומר סטטיסטיקות בסבב 4.
// מי משתמש: FootballManagerService.CycleFour.
// פעולות: GetStatistics ופעולות שמירה ומחיקה.
// תלויות: AccessDatabase, OleDb ו־PlayerStatisticDto.
using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

/// <summary>מאגר סטטיסטיקות C4 בטבלת PlayerStatistics. הוא קורא וממיר שורות DTO, ומבצע הוספה, עדכון ומחיקה דרך OleDb.</summary>
public sealed class AccessPlayerStatisticRepository(AccessDatabase database)
{
    /// <summary>קוראת את שורות PlayerStatistics ומצרפת שמות שחקן ומשחק לצורך התצוגה. כל שורה מומרת ל־PlayerStatisticDto ומוחזרת לשירות.</summary>
    /// <returns>רשימת הרשומות שנשלפו.</returns>
    public List<PlayerStatisticDto> GetStatistics()
    {
        // כל שליפה מתבצעת בצד השרת דרך החיבור שמספק AccessDatabase.
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

    /// <summary>מוסיף שורת סטטיסטיקה חדשה אם StatisticId הוא 0, או מעדכן את השורה הקיימת. פרמטרי OleDb מעבירים את הערכים בנפרד מהפקודה; התוצאה true אם שורה אחת הושפעה.</summary>
    /// <param name="statistic">נתוני משחק או סטטיסטיקה המוצגים או נשמרים.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool Save(PlayerStatisticDto statistic)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        // ההחלטה בין INSERT ל־UPDATE נשענת על מזהה הרשומה שנבחרה במסך.
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

        // התשובה מציינת אם Access שינה שורה אחת.
        return command.ExecuteNonQuery() == 1;
    }

    /// <summary>מוחקת רשומת PlayerStatistics לפי מזהה ומחזירה true אם שורה אחת נמחקה. אם היא לא נמצאה, מחזירה false לשירות.</summary>
    /// <param name="statisticId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool Delete(int statisticId)
    {
        using var connection = database.OpenConnection();
        using var command = new OleDbCommand(
            "DELETE FROM [PlayerStatistics] WHERE [StatisticId] = ?", connection);
        command.Parameters.Add("@StatisticId", OleDbType.Integer).Value = statisticId;
        return command.ExecuteNonQuery() == 1;
    }
}
