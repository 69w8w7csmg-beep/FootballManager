// מטרה: קורא ושומר משתמשים ו־PasswordHash בשרת.
// מי משתמש: FootballManagerService ופקודות ההדגמה ב־Program.
// פעולות: חיפוש חשבון, יצירה, שינוי תפקיד וחסימה.
// תלויות: AccessDatabase, OleDb וטבלת Users.
using System.Data.OleDb;
using FootballManager.Contracts;

namespace FootballManager.WCF.Data;

/// <summary>מודל פנימי של שורת Users בצד השרת. הוא כולל PasswordHash לצורך אימות אך אינו DTO שמוצג למשתמש.</summary>
public sealed class UserRecord
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}

/// <summary>מאגר חשבונות שמבצע שאילתות ושינויים בטבלת Users דרך OleDb. שירות WCF משתמש בו אחרי בדיקות הרשאה; הלקוח אינו ניגש למסד.</summary>
public sealed class AccessUserRepository
{
    private readonly AccessDatabase _database;

    /// <summary>מבצעת את שלב המערכת Access User Repository באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="database">ערך מסוג AccessDatabase שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public AccessUserRepository(AccessDatabase database)
    {
        _database = database;
    }

    /// <summary>נקראת מתהליך Login. מחפשת שורת Users לפי Username בעזרת פרמטר OleDb, וממירה אותה ל־UserRecord כדי שהשירות יבדוק גיבוב, Role וחסימה. מחזירה null אם החשבון לא נמצא.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <returns>ערך מסוג UserRecord? שמשמש את הקוד הקורא.</returns>
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

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Find By Id ומחזירה את התוצאה לשכבה הקוראת. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Delete Managed User, Update User Role.</summary>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>ערך מסוג UserRecord? שמשמש את הקוד הקורא.</returns>
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

    /// <summary>יוצרת חשבון הרשמה ציבורי בתפקיד Player. היא מקבלת PasswordHash שכבר יצר שירות WCF ומחזירה false אם שם המשתמש הייחודי כבר תפוס.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="passwordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool TryCreatePublicAccount(string username, string passwordHash)
    {
        return TryInsertAccount(username, "Player", passwordHash);
    }

    /// <summary>יוצרת חשבון לפי תפקיד שהתקבל מפעולת אדמין מורשית. מוודאת שה־Role שייך ל־Player, Manager או Admin לפני ניסיון כתיבה.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <param name="passwordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool TryCreateManagedAccount(string username, string role, string passwordHash)
    {
        if (role is not ("Player" or "Manager" or "Admin"))
        {
            return false;
        }

        return TryInsertAccount(username, role, passwordHash);
    }

    /// <summary>מבצעת את פעולת הנתונים Create Demo Accounts. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך אתחול.</summary>
    /// <param name="playerPasswordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <param name="managerPasswordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
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

    /// <summary>מבצעת את שלב המערכת Prepare Demo Accounts באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך אתחול.</summary>
    /// <param name="accounts">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    public void PrepareDemoAccounts(IReadOnlyList<(string Username, string Role, string PasswordHash)> accounts)
    {
        using var connection = _database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        foreach (var account in accounts)
        {
            using var check = new OleDbCommand(
                "SELECT COUNT(*) FROM [Users] WHERE [Username] = ?",
                connection,
                transaction);
            check.Parameters.AddWithValue("@Username", account.Username);
            var exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
            using var command = new OleDbCommand(exists
                ? "UPDATE [Users] SET [PasswordHash] = ?, [Role] = ?, [IsBlocked] = False WHERE [Username] = ?"
                : "INSERT INTO [Users] ([PasswordHash], [Role], [IsBlocked], [Username]) VALUES (?, ?, False, ?)",
                connection,
                transaction);
            command.Parameters.AddWithValue("@PasswordHash", account.PasswordHash);
            command.Parameters.AddWithValue("@Role", account.Role);
            command.Parameters.AddWithValue("@Username", account.Username);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    /// <summary>מנסה לבצע את הפעולה Try Insert Account ומחזירה תוצאה שמאפשרת לקוד הקורא לזהות הצלחה או כישלון. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Try Create Public Account, Try Create Managed Account.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <param name="passwordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
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

    /// <summary>מבצעת את שלב המערכת Insert Account באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Create Demo Accounts, Try Insert Account.</summary>
    /// <param name="connection">ערך מסוג OleDbConnection שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="transaction">ערך מסוג OleDbTransaction שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <param name="passwordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
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

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Get All ומחזירה את התוצאה לשכבה הקוראת. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך אתחול, Get Managed Users.</summary>
    /// <returns>רשימת הרשומות שנשלפו.</returns>
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

    /// <summary>מבצעת את שלב המערכת Count Active Admins באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Delete Managed User, Update User Role.</summary>
    /// <returns>ערך מסוג int שמשמש את הקוד הקורא.</returns>
    public int CountActiveAdmins()
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Role] = 'Admin' AND [IsBlocked] = False",
            connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>מבצעת את פעולת הנתונים Update Role. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Update User Role.</summary>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="role">סוג המשתמש או התפקיד המבוקש; השרת מאמת הרשאות לפי החשבון.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Set Blocked. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Set User Blocked.</summary>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <param name="isBlocked">דגל המציין אם המצב או הפעולה מופעלים.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
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

    /// <summary>מבצעת את פעולת הנתונים Delete User. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך Delete Managed User.</summary>
    /// <param name="userId">המזהה המספרי של הרשומה או הקשר המבוקש.</param>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool DeleteUser(int userId)
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "DELETE FROM [Users] WHERE [UserId] = ?",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);
        return command.ExecuteNonQuery() == 1;
    }

    /// <summary>בודקת את התנאי Any Admin Exists ומחזירה תשובת כן או לא לשימוש בקוד הקורא או בממשק. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך אתחול.</summary>
    /// <returns>true או false לפי הצלחת הבדיקה או השינוי.</returns>
    public bool AnyAdminExists()
    {
        using var connection = _database.OpenConnection();
        using var command = new OleDbCommand(
            "SELECT COUNT(*) FROM [Users] WHERE [Role] = 'Admin'",
            connection);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    /// <summary>מבצעת את פעולת הנתונים Create Initial Admin. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה. בגוף הפעולה: ניגשת למסד Access באמצעות OleDb. נקראת בקוד מתוך אתחול.</summary>
    /// <param name="username">שם המשתמש שנקלט או נשמר.</param>
    /// <param name="passwordHash">סיסמה או גיבוב סיסמה, בהתאם לסוג הפרמטר ולשלב ההזדהות.</param>
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

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Read User ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Find By Username, Find By Id.</summary>
    /// <param name="reader">ערך מסוג OleDbDataReader שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>ערך מסוג UserRecord שמשמש את הקוד הקורא.</returns>
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
