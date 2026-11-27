// מטרה: חושף קטלוג ונתוני כדורגל לקריאה מתוך האתר.
// מי משתמש: עמוד Football Data ו־Program שמרשמת את הנתיבים.
// פעולות: בודק מזהים ועונה, וממפה בקשות משחקים, קבוצות, סגלים והרכבים.
// תלויות: FootballFeed והקטלוג המקומי של מערכים.
using FootballManager.WCF.Services;

namespace FootballManager.WCF.Web;

public static class FootballFeedEndpoints
{
    private static readonly Dictionary<int, string> Leagues = new()
    {
        [383] = "ליגת העל בישראל", [39] = "פרמייר ליג", [140] = "לה ליגה",
        [135] = "סרייה א׳", [78] = "בונדסליגה", [61] = "ליג 1"
    };

    /// <summary>מחבר את עמוד הנתונים לספק דרך השרת. הקריאות ציבוריות ומופרדות מרשומות Access.</summary>
    public static void MapFootballFeed(this WebApplication app)
    {
        app.MapGet("/api/football/catalog", (IConfiguration configuration) =>
        {
            var now = DateTime.UtcNow;
            var currentSeason = now.Month < 7 ? now.Year - 1 : now.Year;
            var configuredSeason = configuration.GetValue("FootballFeed:DefaultSeason", currentSeason);
            return Results.Ok(new
            {
                season = configuredSeason >= 2000 && configuredSeason <= now.Year ? configuredSeason : currentSeason,
                leagues = Leagues.Select(entry => new { id = entry.Key, name = entry.Value }),
                formations = LineupEndpoints.Formations
            });
        });
        app.MapGet("/api/football/matches", (int league, int season, FootballFeed feed, CancellationToken token) =>
            ValidSeason(league, season)
                ? ReadAsync(feed, FormattableString.Invariant($"fixtures?league={league}&season={season}&timezone=Asia%2FJerusalem"), 10, token)
                : Task.FromResult(Invalid()));
        app.MapGet("/api/football/teams", (int league, int season, FootballFeed feed, CancellationToken token) =>
            ValidSeason(league, season)
                ? ReadAsync(feed, $"teams?league={league}&season={season}", 60, token)
                : Task.FromResult(Invalid()));
        app.MapGet("/api/football/squad/{team:int}", (int team, FootballFeed feed, CancellationToken token) =>
            team > 0 ? ReadAsync(feed, $"players/squads?team={team}", 60, token) : Task.FromResult(Invalid()));
        app.MapGet("/api/football/lineups/{match:int}", (int match, FootballFeed feed, CancellationToken token) =>
            match > 0 ? ReadAsync(feed, $"fixtures/lineups?fixture={match}", 10, token) : Task.FromResult(Invalid()));
        app.MapGet("/api/football/match-players/{match:int}", (int match, FootballFeed feed, CancellationToken token) =>
            match > 0 ? ReadAsync(feed, $"fixtures/players?fixture={match}", 60, token) : Task.FromResult(Invalid()));
        app.MapGet("/api/football/events/{match:int}", (int match, FootballFeed feed, CancellationToken token) =>
            match > 0 ? ReadAsync(feed, $"fixtures/events?fixture={match}", 10, token) : Task.FromResult(Invalid()));
        app.MapGet("/api/football/player-season", (int player, int league, int season, FootballFeed feed, CancellationToken token) =>
            player > 0 && ValidSeason(league, season)
                ? ReadAsync(feed, $"players?id={player}&league={league}&season={season}", 60, token)
                : Task.FromResult(Invalid()));
        app.MapLineupPlans();
    }

    /// <summary>מגביל חיפושים לליגות שמוצגות ולעונה בת ארבע ספרות בטווח סביר.</summary>
    private static bool ValidSeason(int league, int season) =>
        Leagues.ContainsKey(league) && season >= 2000 && season <= DateTime.UtcNow.Year;

    /// <summary>מחזיר תשובה אחידה כאשר ערך המסנן אינו תקין.</summary>
    private static IResult Invalid() => Results.BadRequest(new { message = "הליגה, העונה או המזהה אינם תקינים." });

    /// <summary>מעביר לעמוד תוצאה מהספק או הודעת שגיאה בטוחה להצגה.</summary>
    private static async Task<IResult> ReadAsync(FootballFeed feed, string path, int cacheMinutes, CancellationToken token)
    {
        try { return Results.Ok(await feed.GetAsync(path, TimeSpan.FromMinutes(cacheMinutes), token)); }
        catch (FootballFeedException error)
        {
            return Results.Json(new { message = error.Message }, statusCode: error.Status);
        }
    }
}
