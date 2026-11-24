// מטרה: קורא ושומר העברות בסבב 4.
// מי משתמש: FootballManagerService.CycleFour.
// פעולות: GetTransfers ופעולות שמירה ומחיקה.
// תלויות: AccessDatabase, OleDb ו־TransferDto.
using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

/// <summary>מאגר העברות C4 בטבלת Transfers. הוא ממיר בין שורות Access ל־TransferDto ומבצע INSERT, UPDATE ו־DELETE לפי מזהה.</summary>
public sealed class AccessTransferRepository(AccessDatabase database)
{
    /// <summary>פותחת חיבור למסד וקוראת את העברות השחקנים עם שמות השחקן והקבוצות לצורך תצוגה. כל רשומה הופכת ל־TransferDto. נקראת מתוך פעולת השליפה של שירות C4.</summary>
    /// <returns>רשימת הרשומות שנשלפו.</returns>
    public List<TransferDto> GetTransfers()
    {
        // החיבור נוצר דרך רכיב המסד המרכזי ומשתחרר בסיום באמצעות using.
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT T.[TransferId], T.[PlayerId], T.[FromTeamId], T.[ToTeamId], " +
            "T.[TransferDate], T.[Price], P.[FirstName], P.[LastName], F.[Name], D.[Name] " +
            "FROM (([Transfers] AS T INNER JOIN [Players] AS P ON T.[PlayerId] = P.[PlayerId]) " +
            "INNER JOIN [Teams] AS F ON T.[FromTeamId] = F.[TeamId]) " +
            "INNER JOIN [Teams] AS D ON T.[ToTeamId] = D.[TeamId] " +
            "ORDER BY T.[TransferDate] DESC, T.[TransferId] DESC";

        using var reader = command.ExecuteReader();
        var transfers = new List<TransferDto>();
        while (reader.Read())
        {
            transfers.Add(new TransferDto
            {
                TransferId = reader.GetInt32(0),
                PlayerId = reader.GetInt32(1),
                FromTeamId = reader.GetInt32(2),
                ToTeamId = reader.GetInt32(3),
                TransferDate = reader.GetDateTime(4),
                Price = Convert.ToDecimal(reader[5]),
                PlayerName = $"{reader.GetString(6)} {reader.GetString(7)}",
                FromTeamName = reader.GetString(8),
                ToTeamName = reader.GetString(9)
            });
        }

        return transfers;
    }

    /// <summary>מוסיף העברה כשהמזהה 0 או מעדכן רשומה קיימת כשהמזהה אינו 0. פרמטרי OleDb מונעים חיבור ערכי טופס ישירות לתוך SQL. מחזיר true אם שורה אחת הושפעה.</summary>
    /// <param name="transfer">נתוני העברת השחקן שעליהם פועלת הפעולה.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool Save(TransferDto transfer)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
        // בחירת פקודת INSERT או UPDATE קובעת אם זו רשומה חדשה או עריכה.
        command.CommandText = transfer.TransferId == 0
            ? "INSERT INTO [Transfers] ([PlayerId], [FromTeamId], [ToTeamId], [TransferDate], [Price]) " +
              "VALUES (?, ?, ?, ?, ?)"
            : "UPDATE [Transfers] SET [PlayerId] = ?, [FromTeamId] = ?, [ToTeamId] = ?, " +
              "[TransferDate] = ?, [Price] = ? WHERE [TransferId] = ?";
        command.Parameters.Add("@PlayerId", OleDbType.Integer).Value = transfer.PlayerId;
        command.Parameters.Add("@FromTeamId", OleDbType.Integer).Value = transfer.FromTeamId;
        command.Parameters.Add("@ToTeamId", OleDbType.Integer).Value = transfer.ToTeamId;
        command.Parameters.Add("@TransferDate", OleDbType.Date).Value = transfer.TransferDate;
        command.Parameters.Add("@Price", OleDbType.Currency).Value = transfer.Price;
        if (transfer.TransferId != 0)
        {
            command.Parameters.Add("@TransferId", OleDbType.Integer).Value = transfer.TransferId;
        }

        // Access מאשר הצלחה אם פקודת השינוי השפיעה על שורה אחת.
        return command.ExecuteNonQuery() == 1;
    }

    /// <summary>מוחקת שורה מטבלת Transfers לפי המזהה ומחזירה true אם נמצאה ונמחקה שורה אחת. קשרי המפתח הזר מתחייבים במסד Access.</summary>
    /// <param name="transferId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool Delete(int transferId)
    {
        using var connection = database.OpenConnection();
        using var command = new OleDbCommand("DELETE FROM [Transfers] WHERE [TransferId] = ?", connection);
        command.Parameters.Add("@TransferId", OleDbType.Integer).Value = transferId;
        return command.ExecuteNonQuery() == 1;
    }
}
