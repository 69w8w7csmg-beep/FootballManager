// מטרה: מאפשר למאמן לנהל תוכנית הרכב אישית ולהזמין המלצה.
// מי משתמש: עורך ההרכבים באתר.
// פעולות: טעינה, שמירה וניתוח לאחר בדיקות הרשאה והרכב.
// תלויות: FootballManagerService, FootballFeed, LineupPlans ו־GeminiChatProvider.
using System.Text.Json;
using FootballManager.WCF.Services;

namespace FootballManager.WCF.Web;

public static class LineupEndpoints
{
    internal static readonly string[] Formations = ["4-3-3", "4-2-2-2", "4-4-2", "4-2-3-1", "3-5-2", "3-4-3", "5-3-2"];

    /// <summary>מגדירה את נקודות הכניסה לתוכניות, שדורשות חשבון מאמן פעיל.</summary>
    public static void MapLineupPlans(this WebApplication app)
    {
        const string path = "/api/football/plans/{match:int}/{team:int}";
        app.MapGet(path, (int match, int team, HttpContext context, FootballManagerService service, LineupPlans plans, CancellationToken token) =>
            ForManagerAsync(context, service, match, team, async owner =>
                Results.Ok(new { plan = await plans.LoadAsync(owner, match, team, token) })));
        app.MapPut(path, (int match, int team, LineupPlan plan, HttpContext context, FootballManagerService service,
            FootballFeed feed, LineupPlans plans, CancellationToken token) =>
            ForManagerAsync(context, service, match, team, async owner =>
            {
                await CheckPlanAsync(feed, match, team, plan, token);
                await plans.SaveAsync(owner, match, team, plan, token);
                return Results.Ok(new { message = "תוכנית ההרכב נשמרה בחשבון שלך." });
            }));
        app.MapPost(path + "/advice", (int match, int team, LineupPlan plan, HttpContext context, FootballManagerService service,
            FootballFeed feed, GeminiChatProvider ai, CancellationToken token) =>
            ForManagerAsync(context, service, match, team, async _ =>
            {
                var data = await CheckPlanAsync(feed, match, team, plan, token);
                var snapshot = await CreateSnapshotAsync(feed, match, data, plan, token);
                return Results.Ok(new { advice = await ai.AskLineupAsync(snapshot, token) });
            }));
    }

    /// <summary>מקבלת את מזהה הבעלים מהאסימון. שגיאת הרשאה חוזרת כ־401 או 403.</summary>
    private static async Task<IResult> ForManagerAsync(HttpContext context, FootballManagerService service,
        int match, int team, Func<int, Task<IResult>> action)
    {
        var owner = service.GetLineupManagerId(context.Request.Cookies["fm_session"] ?? "");
        if (owner is null)
            return Error((int)service.ResponseStatus, service.ResponseStatus == System.Net.HttpStatusCode.Forbidden
                ? "403 · תכנון ההרכב זמין לחשבון מאמן." : "401 · יש להתחבר כמאמן.");
        if (match <= 0 || team <= 0) return Error(400, "יש לבחור משחק וקבוצה תקינים.");
        try { return await action(owner.Value); }
        catch (FootballFeedException error) { return Error(error.Status, error.Message); }
        catch (GeminiChatException error) { return Error((int)error.Status, error.Message); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        { return Error(503, "לא ניתן לגשת לתוכנית ההרכב השמורה כרגע."); }
    }

    /// <summary>מוודאת שהקבוצה שייכת למשחק, ושכל שחקן נבחר פעם אחת מתוך הסגל.</summary>
    private static async Task<PlanData> CheckPlanAsync(FootballFeed feed, int match, int team, LineupPlan plan, CancellationToken token)
    {
        if (!Formations.Contains(plan.Formation) || plan.Starters?.Length != 11 || plan.Bench is null || plan.Bench.Length > 12)
            throw new FootballFeedException(400, "בחר מערך, 11 פותחים ועד 12 מחליפים.");
        var ids = plan.Starters.Concat(plan.Bench).ToArray();
        if (ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Length)
            throw new FootballFeedException(400, "כל שחקן יכול להופיע פעם אחת בלבד בהרכב או בספסל.");

        var result = await feed.GetAsync($"fixtures?id={match}", TimeSpan.FromMinutes(15), token);
        var fixture = result.EnumerateArray().FirstOrDefault();
        var teams = Property(fixture, "teams");
        var home = Int(Property(teams, "home"), "id");
        var away = Int(Property(teams, "away"), "id");
        if (team != home && team != away || home <= 0 || away <= 0)
            throw new FootballFeedException(400, "הקבוצה שנבחרה אינה חלק מהמשחק.");

        var squads = await feed.GetAsync($"players/squads?team={team}", TimeSpan.FromHours(1), token);
        var players = Array(Property(squads.EnumerateArray().FirstOrDefault(), "players"))
            .Select(p => new SquadPlayer(Int(p, "id"), Text(p, "name"), Text(p, "position")))
            .Where(p => p.Id > 0).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
        if (ids.Any(id => !players.ContainsKey(id)))
            throw new FootballFeedException(400, "אחד השחקנים אינו מופיע בסגל הנוכחי של הקבוצה.");
        if (!IsKeeper(players[plan.Starters[0]].Position))
            throw new FootballFeedException(400, "העמדה הראשונה בהרכב שמורה לשוער מהסגל.");

        var league = Property(fixture, "league");
        return new(players, team == home ? away : home, Int(league, "id"), Int(league, "season"));
    }

    /// <summary>מוסיפה נתוני שוער היריבה רק אם הספק פרסם הרכב, ונתוני עונה לכמה מועמדים.</summary>
    private static async Task<string> CreateSnapshotAsync(FootballFeed feed, int match, PlanData data,
        LineupPlan plan, CancellationToken token)
    {
        JsonElement lineups;
        try { lineups = await feed.GetAsync($"fixtures/lineups?fixture={match}", TimeSpan.FromMinutes(10), token); }
        catch (FootballFeedException) { lineups = default; }
        var opposition = Array(lineups).FirstOrDefault(row => Int(Property(row, "team"), "id") == data.Opponent);
        var keeper = Array(Property(opposition, "startXI")).Select(row => Property(row, "player"))
            .FirstOrDefault(player => IsKeeper(Text(player, "pos")));
        var keeperId = Int(keeper, "id");
        var keeperStats = keeperId > 0 ? await StatsAsync(feed, data, keeperId, token) : null;
        var benchStats = new List<object>();
        // מגבילים קריאות כדי לשמור על המכסה. מדובר בנתוני עונה, לא בתחזית של תוצאת המשחק.
        foreach (var id in plan.Bench.Where(id => IsAttacker(data.Players[id].Position)).Take(3))
            benchStats.Add(new { player = data.Players[id], statistics = await StatsAsync(feed, data, id, token) });
        return JsonSerializer.Serialize(new
        {
            plan.Formation,
            starters = plan.Starters.Select(id => data.Players[id]),
            bench = plan.Bench.Select(id => data.Players[id]),
            opponentKeeper = keeperId > 0 ? Text(keeper, "name") : null,
            keeperSeasonStatistics = keeperStats, benchSeasonStatistics = benchStats,
            instruction = "אם שוער היריבה לא ידוע או אין סטטיסטיקה, אין להסיק שהוא חזק. אל תבלבל בין שערים, הצלות ובישולים."
        });
    }

    /// <summary>קוראת נתוני עונה של שחקן אחד; כיסוי חסר נשמר כנתון חסר.</summary>
    private static async Task<object?> StatsAsync(FootballFeed feed, PlanData data, int player, CancellationToken token)
    {
        if (data.Season <= 0 || data.League <= 0) return null;
        try
        {
            var response = await feed.GetAsync($"players?id={player}&league={data.League}&season={data.Season}", TimeSpan.FromHours(12), token);
            var stats = Array(Property(response.EnumerateArray().FirstOrDefault(), "statistics")).FirstOrDefault();
            if (stats.ValueKind != JsonValueKind.Object) return null;
            var goals = Property(stats, "goals");
            return new
            {
                appearances = NullableInt(Property(stats, "games"), "appearences"),
                goals = NullableInt(goals, "total"),
                saves = NullableInt(goals, "saves"),
                shotsOnTarget = NullableInt(Property(stats, "shots"), "on")
            };
        }
        catch (FootballFeedException) { return null; }
    }

    /// <summary>קוראת ערך אופציונלי בלי להפוך חוסר נתון למחרוזת או למספר בדוי.</summary>
    private static JsonElement Property(JsonElement value, string key) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var field) ? field : default;

    private static IEnumerable<JsonElement> Array(JsonElement value) =>
        value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [];
    private static int? NullableInt(JsonElement value, string key) =>
        Property(value, key) is { ValueKind: JsonValueKind.Number } field && field.TryGetInt32(out var number) ? number : null;
    private static int Int(JsonElement value, string key) => NullableInt(value, key) ?? 0;
    private static string Text(JsonElement value, string key) =>
        Property(value, key) is { ValueKind: JsonValueKind.String } field ? field.GetString() ?? "" : "";
    private static bool IsKeeper(string position) => position.Equals("Goalkeeper", StringComparison.OrdinalIgnoreCase) || position == "G";
    private static bool IsAttacker(string position) => position.Equals("Attacker", StringComparison.OrdinalIgnoreCase) || position == "F";
    private static IResult Error(int status, string message) => Results.Json(new { message }, statusCode: status);
    private sealed record SquadPlayer(int Id, string Name, string Position);
    private sealed record PlanData(Dictionary<int, SquadPlayer> Players, int Opponent, int League, int Season);
}
