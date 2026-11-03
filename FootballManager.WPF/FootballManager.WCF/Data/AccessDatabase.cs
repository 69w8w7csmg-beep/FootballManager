using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FootballManager.WCF.Data;

public sealed class AccessDatabase
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public AccessDatabase(IConfiguration configuration, IHostEnvironment environment)
    {
        var relativePath = configuration["AccessDatabase:Path"] ?? "Data/FootballManager.accdb";
        _databasePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
        _connectionString =
            $"Provider=Microsoft.ACE.OLEDB.16.0;Data Source={_databasePath};Persist Security Info=False;";
    }

    public bool IsReady { get; private set; }

    public string DatabasePath => _databasePath;

    public void Initialize()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Access requires Windows and the Microsoft ACE OLE DB provider.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);

        if (!File.Exists(_databasePath))
        {
            CreateAccessFile(_databasePath);
        }

        using var connection = new OleDbConnection(_connectionString);
        connection.Open();

        EnsureTable(connection, "Users",
            "CREATE TABLE [Users] (" +
            "[UserId] COUNTER CONSTRAINT [PK_Users] PRIMARY KEY, " +
            "[Username] TEXT(30) NOT NULL, " +
            "[PasswordHash] TEXT(255) NOT NULL, " +
            "[Role] TEXT(10) NOT NULL, " +
            "[IsBlocked] YESNO NOT NULL, " +
            "CONSTRAINT [CK_Users_Role] CHECK ([Role] IN ('Player', 'Manager', 'Admin')))");
        EnsureUserColumnSizes(connection);
        EnsureUsernameIndex(connection);

        EnsureTable(connection, "Teams",
            "CREATE TABLE [Teams] (" +
            "[TeamId] COUNTER CONSTRAINT [PK_Teams] PRIMARY KEY, " +
            "[Name] TEXT(100) NOT NULL, " +
            "[Country] TEXT(60) NOT NULL, " +
            "[League] TEXT(60) NOT NULL, " +
            "[Budget] CURRENCY NOT NULL)");

        EnsureTable(connection, "Players",
            "CREATE TABLE [Players] (" +
            "[PlayerId] COUNTER CONSTRAINT [PK_Players] PRIMARY KEY, " +
            "[FirstName] TEXT(50) NOT NULL, " +
            "[LastName] TEXT(50) NOT NULL, " +
            "[Age] INTEGER NOT NULL, " +
            "[Position] TEXT(30) NOT NULL, " +
            "[Rating] BYTE NOT NULL, " +
            "[Speed] BYTE NOT NULL, " +
            "[Shooting] BYTE NOT NULL, " +
            "[Passing] BYTE NOT NULL, " +
            "[Defense] BYTE NOT NULL, " +
            "CONSTRAINT [CK_Players_Skills] CHECK (" +
            "[Rating] BETWEEN 0 AND 100 AND [Speed] BETWEEN 0 AND 100 AND " +
            "[Shooting] BETWEEN 0 AND 100 AND [Passing] BETWEEN 0 AND 100 AND " +
            "[Defense] BETWEEN 0 AND 100))");

        EnsureTable(connection, "TeamPlayers",
            "CREATE TABLE [TeamPlayers] (" +
            "[TeamId] LONG NOT NULL, " +
            "[PlayerId] LONG NOT NULL, " +
            "[StartDate] DATETIME NOT NULL, " +
            "[EndDate] DATETIME, " +
            "CONSTRAINT [PK_TeamPlayers] PRIMARY KEY ([TeamId], [PlayerId]), " +
            "CONSTRAINT [FK_TeamPlayers_Teams] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([TeamId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_TeamPlayers_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE)");

        EnsureTable(connection, "Matches",
            "CREATE TABLE [Matches] (" +
            "[MatchId] COUNTER CONSTRAINT [PK_Matches] PRIMARY KEY, " +
            "[HomeTeamId] LONG NOT NULL, " +
            "[AwayTeamId] LONG NOT NULL, " +
            "[MatchDate] DATETIME NOT NULL, " +
            "[HomeScore] INTEGER NOT NULL, " +
            "[AwayScore] INTEGER NOT NULL, " +
            "CONSTRAINT [FK_Matches_HomeTeam] FOREIGN KEY ([HomeTeamId]) REFERENCES [Teams] ([TeamId]), " +
            "CONSTRAINT [FK_Matches_AwayTeam] FOREIGN KEY ([AwayTeamId]) REFERENCES [Teams] ([TeamId]))");

        EnsureTable(connection, "PlayerStatistics",
            "CREATE TABLE [PlayerStatistics] (" +
            "[StatisticId] COUNTER CONSTRAINT [PK_PlayerStatistics] PRIMARY KEY, " +
            "[PlayerId] LONG NOT NULL, " +
            "[MatchId] LONG NOT NULL, " +
            "[Goals] INTEGER NOT NULL, " +
            "[Assists] INTEGER NOT NULL, " +
            "[MinutesPlayed] INTEGER NOT NULL, " +
            "[Rating] SINGLE NOT NULL, " +
            "CONSTRAINT [FK_PlayerStatistics_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]), " +
            "CONSTRAINT [FK_PlayerStatistics_Matches] FOREIGN KEY ([MatchId]) REFERENCES [Matches] ([MatchId]))");

        EnsureTable(connection, "Transfers",
            "CREATE TABLE [Transfers] (" +
            "[TransferId] COUNTER CONSTRAINT [PK_Transfers] PRIMARY KEY, " +
            "[PlayerId] LONG NOT NULL, " +
            "[FromTeamId] LONG NOT NULL, " +
            "[ToTeamId] LONG NOT NULL, " +
            "[TransferDate] DATETIME NOT NULL, " +
            "[Price] CURRENCY NOT NULL, " +
            "CONSTRAINT [FK_Transfers_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]), " +
            "CONSTRAINT [FK_Transfers_FromTeam] FOREIGN KEY ([FromTeamId]) REFERENCES [Teams] ([TeamId]), " +
            "CONSTRAINT [FK_Transfers_ToTeam] FOREIGN KEY ([ToTeamId]) REFERENCES [Teams] ([TeamId]))");

        EnsureTable(connection, "Favorites",
            "CREATE TABLE [Favorites] (" +
            "[UserId] LONG NOT NULL, " +
            "[PlayerId] LONG NOT NULL, " +
            "CONSTRAINT [PK_Favorites] PRIMARY KEY ([UserId], [PlayerId]), " +
            "CONSTRAINT [FK_Favorites_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_Favorites_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE)");

        IsReady = true;
    }

    public OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void EnsureTable(OleDbConnection connection, string tableName, string createSql)
    {
        var schema = connection.GetSchema("Tables");
        var exists = schema.Rows
            .Cast<DataRow>()
            .Any(row => string.Equals(
                row["TABLE_NAME"]?.ToString(),
                tableName,
                StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            using var command = new OleDbCommand(createSql, connection);
            command.ExecuteNonQuery();
        }
    }

    private static void EnsureUserColumnSizes(OleDbConnection connection)
    {
        EnsureTextColumnSize(connection, "Username", 30);
        EnsureTextColumnSize(connection, "Role", 10);
    }

    private static void EnsureTextColumnSize(OleDbConnection connection, string columnName, int maximumLength)
    {
        var columns = connection.GetSchema("Columns");
        var column = columns.Rows
            .Cast<DataRow>()
            .FirstOrDefault(row =>
                string.Equals(row["TABLE_NAME"]?.ToString(), "Users", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(row["COLUMN_NAME"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase));

        if (column is null || column.IsNull("CHARACTER_MAXIMUM_LENGTH"))
        {
            throw new InvalidOperationException($"לא נמצאה עמודת Users.{columnName} במסד.");
        }

        var currentLength = Convert.ToInt32(column["CHARACTER_MAXIMUM_LENGTH"]);
        if (currentLength > maximumLength)
        {
            using var command = new OleDbCommand(
                $"ALTER TABLE [Users] ALTER COLUMN [{columnName}] TEXT({maximumLength})",
                connection);
            command.ExecuteNonQuery();
        }
    }

    private static void EnsureUsernameIndex(OleDbConnection connection)
    {
        var indexes = connection.GetSchema("Indexes");
        var indexExists = indexes.Rows
            .Cast<DataRow>()
            .Any(row =>
                string.Equals(row["TABLE_NAME"]?.ToString(), "Users", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(row["INDEX_NAME"]?.ToString(), "UX_Users_Username", StringComparison.OrdinalIgnoreCase));

        if (!indexExists)
        {
            using var command = new OleDbCommand(
                "CREATE UNIQUE INDEX [UX_Users_Username] ON [Users] ([Username])",
                connection);
            command.ExecuteNonQuery();
        }
    }

    private static void CreateAccessFile(string path)
    {
        var catalogType = Type.GetTypeFromProgID("ADOX.Catalog");
        if (catalogType is null)
        {
            throw new InvalidOperationException(
                "ADOX is not installed. Install or repair the Microsoft Access Database Engine.");
        }

        var catalog = Activator.CreateInstance(catalogType)
            ?? throw new InvalidOperationException("Could not create the ADOX catalog object.");

        try
        {
            catalogType.InvokeMember(
                "Create",
                BindingFlags.InvokeMethod,
                binder: null,
                target: catalog,
                args: new object[]
                {
                    $"Provider=Microsoft.ACE.OLEDB.16.0;Data Source={path};"
                });
        }
        finally
        {
            if (Marshal.IsComObject(catalog))
            {
                Marshal.FinalReleaseComObject(catalog);
            }
        }
    }
}
