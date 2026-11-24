// מטרה: מביא נתוני ליגות, משחקים, סגלים והרכבים מ־API-Football.
// מי משתמש: נקודות הקריאה של FootballFeedEndpoints בצד השרת.
// פעולות: GetAsync בודקת תצורה, שולחת בקשה ושומרת תשובה בזיכרון.
// תלויות: HttpClient, מטמון השרת ומפתח API במשתנה סביבה.
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace FootballManager.WCF.Services;

public sealed class FootballFeed(HttpClient http, IMemoryCache cache)
{
    /// <summary>מקבלת נתיב פנימי שנבחר בשרת. מחזירה מערך נתונים ציבוריים מהספק.</summary>
    public async Task<JsonElement> GetAsync(string path, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var key = Environment.GetEnvironmentVariable("FOOTBALLMANAGER_API_FOOTBALL_KEY")?.Trim();
        if (string.IsNullOrEmpty(key))
            throw new FootballFeedException(503, "יש להגדיר מפתח API-Football בשרת.");

        var cacheKey = "football-feed:" + path;
        if (cache.TryGetValue<JsonElement>(cacheKey, out var cached)) return cached;

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        // המפתח נשלח בכותרת HTTPS רק לספק. הוא אינו חלק מהתשובה לדפדפן.
        request.Headers.Add("x-apisports-key", key);
        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.TooManyRequests)
                throw new FootballFeedException(429, "מגבלת הבקשות של ספק הנתונים הושגה. נסה שוב מאוחר יותר.");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new FootballFeedException(503, "הספק דחה את הגדרות הגישה של השרת.");
            if (!response.IsSuccessStatusCode)
                throw new FootballFeedException(502, "ספק הנתונים לא הצליח להחזיר תוצאה.");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = payload.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new FootballFeedException(502, "התקבלה תשובה במבנה לא צפוי.");
            if (root.TryGetProperty("errors", out var errors) &&
                ((errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0) ||
                 (errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())))
            {
                // הספק עשוי להחזיר שגיאה גם עם HTTP 200, למשל עונה שאינה זמינה במנוי.
                throw new FootballFeedException(502, "הספק לא אישר את הבקשה. בדוק בלוח החשבון את העונה, הכיסוי ומכסת הבקשות.");
            }
            if (!root.TryGetProperty("response", out var items) || items.ValueKind != JsonValueKind.Array)
                throw new FootballFeedException(502, "חסר מערך נתונים בתשובת הספק.");
            return cache.Set(cacheKey, items.Clone(), lifetime);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FootballFeedException(504, "ספק הנתונים לא ענה בזמן.");
        }
        catch (Exception error) when (error is HttpRequestException or JsonException)
        {
            throw new FootballFeedException(502, "לא התקבלה תשובה תקינה מספק הנתונים.");
        }
    }
}

/// <summary>קוד HTTP והודעה שאפשר להציג בלי לחשוף את מפתח השרת.</summary>
public sealed class FootballFeedException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
