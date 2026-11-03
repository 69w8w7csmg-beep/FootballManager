using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

public sealed class UserRecord
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}

public sealed class AccessUserRepository
{
    private readonly AccessDatabase _database;

    public AccessUserRepository(AccessDatabase database)
    {
        _database = database;
    }

    public UserRecord? FindByUsername(string username)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT [UserId], [Username], [PasswordHash], [Role], [IsBlocked] FROM [Users] WHERE [Username] = ?",
            connection);
        command.Parameters.AddWithValue("@Username", username);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    public UserRecord? FindById(int userId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT [UserId], [Username], [PasswordHash], [Role], [IsBlocked] FROM [Users] WHERE [UserId] = ?",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    public bool TryCreatePublicAccount(string username, string passwordHash)
    {
        return TryInsertAccount(username, "Player", passwordHash);
    }

    public bool TryCreateManagedAccount(string username, string role, string passwordHash)
    {
        if (role is not ("Player" or "Manager" or "Admin"))
        {
            return false;
        }

        return TryInsertAccount(username, role, passwordHash);
    }

    public void CreateDemoAccounts(string playerPasswordHash, string managerPasswordHash)
    {
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var check = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Username] IN (?, ?)",
            connection,
            transaction))
        {
            check.Parameters.AddWithValue("@PlayerUsername", "DemoPlayer");
            check.Parameters.AddWithValue("@ManagerUsername", "DemoManager");
            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            {
                transaction.Rollback();
                throw new InvalidOperationException(
                    "חשבונות ההדגמה כבר קיימים. לא שיניתי סיסמאות או חשבונות קיימים.");
            }
        }

        InsertAccount(connection, transaction, "DemoPlayer", "Player", playerPasswordHash);
        InsertAccount(connection, transaction, "DemoManager", "Manager", managerPasswordHash);
        transaction.Commit();
    }

    private bool TryInsertAccount(string username, string role, string passwordHash)
    {
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using (var check = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Username] = ?",
            connection,
            transaction))
        {
            check.Parameters.AddWithValue("@Username", username);
            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            {
                transaction.Rollback();
                return false;
            }
        }

        InsertAccount(connection, transaction, username, role, passwordHash);

        transaction.Commit();
        return true;
    }

    private static void InsertAccount(
        OleDbConnection connection,
        OleDbTransaction transaction,
        string username,
        string role,
        string passwordHash)
    {
        using var insert = new OleDbCommand(
            "INSERT INTO [Users] ([Username], [PasswordHash], [Role], [IsBlocked]) VALUES (?, ?, ?, False)",
            connection,
            transaction);
        insert.Parameters.AddWithValue("@Username", username);
        insert.Parameters.AddWithValue("@PasswordHash", passwordHash);
        insert.Parameters.AddWithValue("@Role", role);
        insert.ExecuteNonQuery();
    }

    public IReadOnlyList<ManagedUserDto> GetAll()
    {
        var users = new List<ManagedUserDto>();
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT [UserId], [Username], [Role], [IsBlocked] FROM [Users] ORDER BY [Username]",
            connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new ManagedUserDto
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                Username = Convert.ToString(reader["Username"]) ?? string.Empty,
                Role = Convert.ToString(reader["Role"]) ?? string.Empty,
                IsBlocked = Convert.ToBoolean(reader["IsBlocked"])
            });
        }

        return users;
    }

    public int CountActiveAdmins()
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Role] = 'Admin' AND [IsBlocked] = False",
            connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public bool UpdateRole(int userId, string role)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "UPDATE [Users] SET [Role] = ? WHERE [UserId] = ?",
            connection);
        command.Parameters.AddWithValue("@Role", role);
        command.Parameters.AddWithValue("@UserId", userId);
        return command.ExecuteNonQuery() == 1;
    }

    public bool SetBlocked(int userId, bool isBlocked)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "UPDATE [Users] SET [IsBlocked] = ? WHERE [UserId] = ?",
            connection);
        command.Parameters.AddWithValue("@IsBlocked", isBlocked);
        command.Parameters.AddWithValue("@UserId", userId);
        return command.ExecuteNonQuery() == 1;
    }

    public bool DeleteUser(int userId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "DELETE FROM [Users] WHERE [UserId] = ?",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        return command.ExecuteNonQuery() == 1;
    }

    public bool AnyAdminExists()
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Role] = 'Admin'",
            connection);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void CreateInitialAdmin(string username, string passwordHash)
    {
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using (var check = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Role] = 'Admin'",
            connection,
            transaction))
        {
            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
            {
                transaction.Rollback();
                throw new InvalidOperationException(
                    "Admin כבר קיים. לא ניתן ליצור Admin ראשוני נוסף דרך כלי הפיתוח.");
            }
        }

        using (var insert = new OleDbCommand(
            "INSERT INTO [Users] ([Username], [PasswordHash], [Role], [IsBlocked]) VALUES (?, ?, 'Admin', False)",
            connection,
            transaction))
        {
            insert.Parameters.AddWithValue("@Username", username);
            insert.Parameters.AddWithValue("@PasswordHash", passwordHash);
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static UserRecord ReadUser(OleDbDataReader reader)
    {
        return new UserRecord
        {
            UserId = Convert.ToInt32(reader["UserId"]),
            Username = Convert.ToString(reader["Username"]) ?? string.Empty,
            PasswordHash = Convert.ToString(reader["PasswordHash"]) ?? string.Empty,
            Role = Convert.ToString(reader["Role"]) ?? string.Empty,
            IsBlocked = Convert.ToBoolean(reader["IsBlocked"])
        };
    }
}
