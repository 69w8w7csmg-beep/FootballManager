// קריאה, שמירה ומחיקה של העברות בטבלת Transfers הקיימת במסד Access.
using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

public sealed class AccessTransferRepository(AccessDatabase database)
{
    public List<TransferDto> GetTransfers()
    {
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

    public bool Save(TransferDto transfer)
    {
        using var connection = database.OpenConnection();
        using var command = connection.CreateCommand();
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

        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(int transferId)
    {
        using var connection = database.OpenConnection();
        using var command = new OleDbCommand("DELETE FROM [Transfers] WHERE [TransferId] = ?", connection);
        command.Parameters.Add("@TransferId", OleDbType.Integer).Value = transferId;
        return command.ExecuteNonQuery() == 1;
    }
}
