// מטרה: מקים את שמונה הטבלאות ופותח חיבור Access בשרת.
// מי משתמש: Program ומאגרי Access.
// פעולות: Initialize, EnsureTable ו־OpenConnection.
// תלויות: OleDb, ACE 16 וקובץ FootballManager.accdb.
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FootballManager.WCF.Data;

/// <summary>רכיב תשתית שרת WCF שמחשב את נתיב קובץ Access, יוצר/מוודא את הטבלאות והאינדקסים, ופותח חיבורי OleDb. רק שכבת השרת משתמשת בו.</summary>
public sealed class AccessDatabase
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    /// <summary>מבצעת את שלב המערכת Access Database באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="configuration">ערך מסוג IConfiguration שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="environment">ערך מסוג IHostEnvironment שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public AccessDatabase(IConfiguration configuration, IHostEnvironment environment)
    {
        var relativePath = configuration["AccessDatabase:Path"] ?? "Data/FootballManager.accdb";
        _databasePath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
        _connectionString =
            $"Provider=Microsoft.ACE.OLEDB.16.0;Data Source={_databasePath};Persist Security Info=False;";
    }

    public bool IsReady { get; private set; }

    public string DatabasePath => _databasePath;

    /// <summary>נקראת בעת אתחול WCF. היא מוודאת שקובץ Access קיים, פותחת חיבור ומוודאת את Users, Teams, Players, TeamPlayers, Matches, PlayerStatistics, Transfers ו־Favorites יחד עם המפתחות והאינדקסים. בסיום IsReady מציין שהמסד מוכן; שגיאת ספק או מערכת הפעלה נזרקת ומדווחת באתחול השירות.</summary>
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

        // המשתמשים נשמרים עם PasswordHash ותפקיד; אורח אינו מקבל שורת Users.
        // סבב 1: מפתח ראשי UserId. אין מפתחות זרים בטבלת החשבונות.
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

        // סבב 2: קבוצות ותקציב, מפתח ראשי TeamId. הטבלה אינה מפנה לטבלה אחרת.
        EnsureTable(connection, "Teams",
            "CREATE TABLE [Teams] (" +
            "[TeamId] COUNTER CONSTRAINT [PK_Teams] PRIMARY KEY, " +
            "[Name] TEXT(100) NOT NULL, " +
            "[Country] TEXT(60) NOT NULL, " +
            "[League] TEXT(60) NOT NULL, " +
            "[Budget] CURRENCY NOT NULL)");

        // סבב 2: פרופיל ודירוגי שחקן, מפתח ראשי PlayerId. השיוך לקבוצה נשמר בנפרד.
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

        // טבלת קישור זו מממשת קשר רבים-לרבים בין שחקנים לקבוצות.
        // סבב 2: המפתח מורכב מ־TeamId ו־PlayerId, שהם גם מפתחות זרים ל־Teams ו־Players.
        EnsureTable(connection, "TeamPlayers",
            "CREATE TABLE [TeamPlayers] (" +
            "[TeamId] LONG NOT NULL, " +
            "[PlayerId] LONG NOT NULL, " +
            "[StartDate] DATETIME NOT NULL, " +
            "[EndDate] DATETIME, " +
            "CONSTRAINT [PK_TeamPlayers] PRIMARY KEY ([TeamId], [PlayerId]), " +
            "CONSTRAINT [FK_TeamPlayers_Teams] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([TeamId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_TeamPlayers_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE)");

        // סבב 3: תאריך ותוצאה, מפתח ראשי MatchId. שני מפתחות זרים ל־Teams מפרידים בית וחוץ.
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

        // נתוני C4 תלויים בשחקן ובמשחק באמצעות Foreign Keys קיימים.
        // מפתח ראשי StatisticId. ‏PlayerId מפנה ל־Players ו־MatchId מפנה ל־Matches.
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

        // כל העברה מחוברת לשחקן ולקבוצת המקור והיעד.
        // סבב 4: מפתח ראשי TransferId. ‏PlayerId מפנה ל־Players ושתי הקבוצות מפנות ל־Teams.
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

        // סבב 2: מועדף אישי. המפתח UserId ו־PlayerId גם מקשר ל־Users ול־Players ומונע כפילויות.
        EnsureTable(connection, "Favorites",
            "CREATE TABLE [Favorites] (" +
            "[UserId] LONG NOT NULL, " +
            "[PlayerId] LONG NOT NULL, " +
            "CONSTRAINT [PK_Favorites] PRIMARY KEY ([UserId], [PlayerId]), " +
            "CONSTRAINT [FK_Favorites_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE, " +
            "CONSTRAINT [FK_Favorites_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE)");

        IsReady = true;
    }

    /// <summary>פותחת חיבור OleDb לקובץ Access שהוגדר בתצורת השרת ומחזירה אותו למאגר שקרא לה. הקוד הקורא אחראי לסגור אותו, בדרך כלל באמצעות using.</summary>
    /// <returns>ערך מסוג OleDbConnection שמשמש את הקוד הקורא.</returns>
    public OleDbConnection OpenConnection()
    {
        var connection = new OleDbConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>מכינה או מאתחלת את רכיב המערכת המתואר בשם Ensure Table לפני השימוש בו. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Initialize.</summary>
    /// <param name="connection">ערך מסוג OleDbConnection שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="tableName">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="createSql">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
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

    /// <summary>מכינה או מאתחלת את רכיב המערכת המתואר בשם Ensure User Column Sizes לפני השימוש בו. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Initialize.</summary>
    /// <param name="connection">ערך מסוג OleDbConnection שהפעולה צריכה כדי לבצע את תפקידה.</param>
    private static void EnsureUserColumnSizes(OleDbConnection connection)
    {
        EnsureTextColumnSize(connection, "Username", 30);
        EnsureTextColumnSize(connection, "Role", 10);
    }

    /// <summary>מכינה או מאתחלת את רכיב המערכת המתואר בשם Ensure Text Column Size לפני השימוש בו. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Ensure User Column Sizes.</summary>
    /// <param name="connection">ערך מסוג OleDbConnection שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="columnName">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <param name="maximumLength">ערך מסוג int שהפעולה צריכה כדי לבצע את תפקידה.</param>
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

    /// <summary>מכינה או מאתחלת את רכיב המערכת המתואר בשם Ensure Username Index לפני השימוש בו. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Initialize.</summary>
    /// <param name="connection">ערך מסוג OleDbConnection שהפעולה צריכה כדי לבצע את תפקידה.</param>
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

    /// <summary>מבצעת את פעולת הנתונים Create Access File. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. נקראת בקוד מתוך Initialize.</summary>
    /// <param name="path">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
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
