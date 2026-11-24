// מטרה: מפעיל בשרת את WCF, אתר הלקוח ופקודות תחזוקת המסד.
// מי משתמש: dotnet run, הלקוחות ופקודות תחזוקה.
// פעולות: רישום השירותים, מיפוי הכתובות והכנת חשבונות דמה.
// תלויות: CoreWCF, AccessDatabase ו־FootballManagerWebApi.
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Channels;
using CoreWCF.Description;
using System.Security.Cryptography;
using FootballManager.WCF.Contracts;
using FootballManager.WCF.Data;
using FootballManager.WCF.Services;
using FootballManager.WCF.Web;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
// השירות מקומי לצורכי הפרויקט ומאזין לכתובת שהלקוח WPF משתמש בה.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5152);
});

var database = new AccessDatabase(builder.Configuration, builder.Environment);
// לפני הפעלת השירות מוודאים שמסד Access והטבלאות קיימים.
database.Initialize();

if (args.Contains("--create-admin", StringComparer.OrdinalIgnoreCase))
{
    CreateInitialAdmin(database);
    return;
}

if (args.Contains("--seed-demo-users", StringComparer.OrdinalIgnoreCase))
{
    CreateDemoUsers(database);
    return;
}

if (args.Contains("--prepare-demo-accounts", StringComparer.OrdinalIgnoreCase))
{
    PrepareDemoAccounts(database);
    return;
}

if (args.Contains("--seed-demo-content", StringComparer.OrdinalIgnoreCase))
{
    new AccessFootballRepository(database).SeedDemoContent();
    Console.WriteLine("נתוני ההדגמה לקבוצות, שחקנים וסגלים הוכנסו (או כבר קיימים).");
    return;
}

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton(database);
builder.Services.AddSingleton<AccessUserRepository>();
builder.Services.AddSingleton<AccessFootballRepository>();
builder.Services.AddSingleton<AccessMatchRepository>();
builder.Services.AddSingleton<AccessTransferRepository>();
builder.Services.AddSingleton<AccessPlayerStatisticRepository>();
builder.Services.AddSingleton<PasswordHasher<UserRecord>>();
builder.Services.AddSingleton<LoginSessionStore>();
builder.Services.AddSingleton<AiChatHistoryStore>();
builder.Services.AddHttpClient<GeminiChatProvider>(client => client.Timeout = TimeSpan.FromSeconds(25));
builder.Services.AddHttpClient<OpenAiPlayerAnalysisProvider>(client =>
{
    // זמן קצוב קצר משאיר את חלון הלקוח זמין גם כששירות AI אינו עונה.
    client.Timeout = TimeSpan.FromSeconds(25);
});
builder.Services.AddTransient<PlayerAnalysisProvider>(services =>
    new GeminiPlayerAnalysisProvider(services.GetRequiredService<GeminiChatProvider>()));
builder.Services.AddTransient<FootballManagerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<FootballManagerService>();
    serviceBuilder.AddServiceEndpoint<FootballManagerService, IFootballManagerService>(
        new BasicHttpBinding(BasicHttpSecurityMode.None),
        "/FootballManagerService");

    var metadata = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    metadata.HttpsGetEnabled = true;
    metadata.HttpGetEnabled = false;
});

app.MapFootballManagerWebApi();

app.Run();

/// <summary>פקודת תחזוקה ליצירת Admin ראשוני כשמריצים את השרת עם --create-admin. היא מבקשת שם משתמש וסיסמה, בודקת אותם, מגבבת את הסיסמה ושומרת דרך AccessUserRepository. אם כבר קיים Admin או שהקלט לא תקין, הפעולה נעצרת עם הודעה.</summary>
/// <param name="database">רכיב AccessDatabase שהשרת אתחל עבור פעולת התחזוקה.</param>
static void CreateInitialAdmin(AccessDatabase database)
{
    var repository = new AccessUserRepository(database);
    if (repository.AnyAdminExists())
    {
        Console.WriteLine("כבר קיים חשבון Admin. לא נוצר חשבון נוסף.");
        return;
    }

    Console.WriteLine("יצירת Admin ראשוני מקומית. הסיסמה לא תוצג ולא תישמר בקוד.");
    string username;
    while (true)
    {
        Console.Write("שם משתמש (3-30 תווים באנגלית/ספרות/_): ");
        username = Console.ReadLine()?.Trim() ?? string.Empty;
        var error = AccountRules.ValidateUsername(username);
        if (error is null)
        {
            break;
        }

        Console.WriteLine(error);
    }

    var password = ReadPassword("סיסמה (8-128 תווים): ");
    var passwordError = AccountRules.ValidatePassword(password);
    if (passwordError is not null)
    {
        Console.WriteLine(passwordError);
        return;
    }

    var confirmation = ReadPassword("הקלד את הסיסמה שוב: ");
    if (!string.Equals(password, confirmation, StringComparison.Ordinal))
    {
        Console.WriteLine("הסיסמאות אינן תואמות.");
        return;
    }

    var user = new UserRecord { Username = username, Role = "Admin" };
    var hash = new PasswordHasher<UserRecord>().HashPassword(user, password);
    try
    {
        repository.CreateInitialAdmin(username, hash);
        Console.WriteLine("חשבון Admin נוצר בהצלחה. כעת אפשר להפעיל את שירות WCF.");
    }
    catch (InvalidOperationException exception)
    {
        Console.WriteLine(exception.Message);
    }
}

/// <summary>פקודת הדגמה ליצירת משתמש Player ו־Manager עם סיסמאות אקראיות, כשמריצים --seed-demo-users. מוודאת שקיים Admin, מגבבת ושומרת את הסיסמאות ומציגה אותן פעם אחת למסך התחזוקה.</summary>
/// <param name="database">רכיב AccessDatabase שהשרת אתחל עבור פעולת התחזוקה.</param>
static void CreateDemoUsers(AccessDatabase database)
{
    var repository = new AccessUserRepository(database);
    if (!repository.AnyAdminExists())
    {
        Console.WriteLine("יש ליצור Admin מקומי לפני יצירת משתמשי הדגמה.");
        return;
    }

    var playerPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    var managerPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    var hasher = new PasswordHasher<UserRecord>();
    var player = new UserRecord { Username = "DemoPlayer", Role = "Player" };
    var manager = new UserRecord { Username = "DemoManager", Role = "Manager" };
    var playerHash = hasher.HashPassword(player, playerPassword);
    var managerHash = hasher.HashPassword(manager, managerPassword);

    try
    {
        repository.CreateDemoAccounts(playerHash, managerHash);
        Console.WriteLine("נוצרו שני חשבונות הדגמה. הסיסמאות מוצגות פעם אחת בלבד:");
        Console.WriteLine($"DemoPlayer: {playerPassword}");
        Console.WriteLine($"DemoManager: {managerPassword}");
        Console.WriteLine("שמור אותן במקום פרטי; במסד נשמרו hashes בלבד.");
    }
    catch (InvalidOperationException exception)
    {
        Console.WriteLine(exception.Message);
    }
}

/// <summary>מאפסת חשבונות הדגמה בשלושת התפקידים כשמריצים --prepare-demo-accounts. היא משאירה Admin קיים, יוצרת סיסמאות חדשות, שומרת רק PasswordHash ומציגה את הסיסמאות פעם אחת כדי לשמור אותן מחוץ למסד.</summary>
/// <param name="database">רכיב AccessDatabase שהשרת אתחל עבור פעולת התחזוקה.</param>
static void PrepareDemoAccounts(AccessDatabase database)
{
    var repository = new AccessUserRepository(database);
    var hasher = new PasswordHasher<UserRecord>();
    var existingAdmin = repository.GetAll().FirstOrDefault(user => user.Role == "Admin" && user.Username != "DemoAdmin")
        ?? repository.GetAll().FirstOrDefault(user => user.Role == "Admin");
    if (existingAdmin is null)
    {
        Console.WriteLine("יש ליצור Admin לפני הכנת חשבונות ההדגמה.");
        return;
    }

    var accounts = new[]
    {
        (Username: existingAdmin.Username, Role: "Admin"),
        (Username: "DemoManager", Role: "Manager"),
        (Username: "DemoPlayer", Role: "Player")
    };
    var credentials = accounts.Select(item =>
    {
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(10));
        var user = new UserRecord { Username = item.Username, Role = item.Role };
        return (item.Username, item.Role, Password: password, Hash: hasher.HashPassword(user, password));
    }).ToArray();

    repository.PrepareDemoAccounts(credentials.Select(item => (item.Username, item.Role, item.Hash)).ToArray());
    Console.WriteLine("חשבונות הדגמה מוכנים. שמור את הסיסמאות הבאות בנפרד מה-ZIP:");
    foreach (var item in credentials)
    {
        Console.WriteLine($"{item.Username}: {item.Password}");
    }
}

/// <summary>קוראת סיסמה מהקונסול בלי להציג את התווים עצמם. Backspace מוחק תו אחרון, Enter מסיים, ומוחזרת הסיסמה בזיכרון לפעולת התחזוקה הקוראת.</summary>
/// <param name="prompt">הטקסט שמסביר למפעיל איזה נתון להקליד.</param>
/// <returns>הסיסמה שהוקלדה; היא מיועדת לגיבוב מיד לאחר הקריאה.</returns>
static string ReadPassword(string prompt)
{
    Console.Write(prompt);
    var characters = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (characters.Count > 0)
            {
                characters.RemoveAt(characters.Count - 1);
                Console.Write("\b \b");
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            characters.Add(key.KeyChar);
            Console.Write('*');
        }
    }

    return new string(characters.ToArray());
}

