using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Channels;
using CoreWCF.Description;
using System.Security.Cryptography;
using FootballManager.WCF.Contracts;
using FootballManager.WCF.Data;
using FootballManager.WCF.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5152);
});

var database = new AccessDatabase(builder.Configuration, builder.Environment);
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

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton(database);
builder.Services.AddSingleton<AccessUserRepository>();
builder.Services.AddSingleton<AccessFootballRepository>();
builder.Services.AddSingleton<PasswordHasher<UserRecord>>();
builder.Services.AddSingleton<LoginSessionStore>();
builder.Services.AddTransient<FootballManagerService>();

var app = builder.Build();

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

app.Run();

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

