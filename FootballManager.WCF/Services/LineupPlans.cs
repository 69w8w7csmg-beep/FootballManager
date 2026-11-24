// מטרה: שומר תוכניות פרטיות למשחק לפי חשבון מאמן.
// מי משתמש: FootballFeedEndpoints אחרי בדיקת הרשאות.
// פעולות: טעינת תוכנית ושמירה אטומית של תוכנית.
// תלויות: JSON ונתיב מקומי של תהליך השרת.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FootballManager.WCF.Data;

namespace FootballManager.WCF.Services;

public sealed record LineupPlan(string Formation, int[] Starters, int[] Bench);

public sealed class LineupPlans(AccessDatabase database, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _writeGate = new(1);

    private readonly string _directory = Path.Combine(
        configuration["Lineups:Directory"] ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FootballManager", "Lineups"),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database.DatabasePath.ToUpperInvariant()))));

    /// <summary>טוענת תוכנית לפי מזהה החשבון שנקבע בשרת, מזהה משחק ומזהה קבוצה.</summary>
    public async Task<LineupPlan?> LoadAsync(int owner, int match, int team, CancellationToken token)
    {
        var path = PathFor(owner, match, team);
        if (!File.Exists(path)) return null;
        // הקורא יכול לסיים לקרוא את התוכנית הקודמת בזמן שגרסה חדשה מחליפה אותה.
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        return await JsonSerializer.DeserializeAsync<LineupPlan>(stream, Json, token);
    }

    /// <summary>כותבת לקובץ זמני ומחליפה את הקובץ לאחר כתיבה מלאה כדי למנוע תוכנית חלקית.</summary>
    public async Task SaveAsync(int owner, int match, int team, LineupPlan plan, CancellationToken token)
    {
        var path = PathFor(owner, match, team);
        var temporary = path + ".tmp";
        await _writeGate.WaitAsync(token);
        try
        {
            Directory.CreateDirectory(_directory);
            await using (var stream = File.Create(temporary))
                await JsonSerializer.SerializeAsync(stream, plan, Json, token);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            finally { _writeGate.Release(); }
        }
    }

    /// <summary>בונה שם מהמספרים שקבע השרת בלבד.</summary>
    private string PathFor(int owner, int match, int team) => Path.Combine(_directory, $"{owner}-{match}-{team}.json");
}
