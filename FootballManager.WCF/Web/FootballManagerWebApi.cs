using FootballManager.Contracts;
using FootballManager.WCF.Services;

namespace FootballManager.WCF.Web;

/// <summary>נקודות REST של האתר הקיים מ־C2. הן משתמשות באותו שירות משתמשים, ובסבב C4 לא מוסיפים להן מסכי תוכן.</summary>
public static class FootballManagerWebApi
{
    private const string SessionCookie = "fm_session";

    /// <summary>מבצעת את שלב המערכת Map Football Manager Web Api באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך אתחול.</summary>
    /// <param name="app">ערך מסוג WebApplication שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public static void MapFootballManagerWebApi(this WebApplication app)
    {
        app.MapGet("/api/status", (IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.GetServiceStatus())));

        app.MapGet("/api/session", (HttpContext context, IServiceProvider services) =>
        {
            var result = WithService(services, service => service.GetCurrentSession(Token(context)));
            return result.Succeeded ? Results.Ok(PublicSession(result)) : Results.Unauthorized();
        });

        app.MapPost("/api/auth/register", (RegisterRequestDto request, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.Register(request))));

        app.MapPost("/api/auth/login", (LoginRequestDto request, HttpContext context, IServiceProvider services) =>
        {
            var result = WithService(services, service => service.Login(request));
            if (!result.Succeeded)
            {
                return Results.Ok(PublicSession(result));
            }

            context.Response.Cookies.Append(SessionCookie, result.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                Path = "/",
                MaxAge = TimeSpan.FromHours(8)
            });
            return Results.Ok(PublicSession(result));
        });

        app.MapPost("/api/auth/logout", (HttpContext context, IServiceProvider services) =>
        {
            var result = WithService(services, service => service.Logout(Token(context)));
            context.Response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/", SameSite = SameSiteMode.Strict });
            return Results.Ok(result);
        });

        app.MapGet("/api/players", (
            string? searchText,
            string? position,
            int? minimumRating,
            int? maximumRating,
            IServiceProvider services) => Results.Ok(WithService(services, service =>
                service.GetPlayers(searchText ?? string.Empty, position ?? string.Empty,
                    minimumRating ?? 0, maximumRating ?? 100))));

        app.MapGet("/api/teams", (IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.GetTeams())));

        app.MapGet("/api/teams/{teamId:int}/roster", (int teamId, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.GetTeamRoster(teamId))));

        app.MapGet("/api/favorites", (HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.GetMyFavorites(Token(context)))));

        app.MapPost("/api/favorites/{playerId:int}", (int playerId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.AddFavorite(Token(context), playerId))));

        app.MapDelete("/api/favorites/{playerId:int}", (int playerId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.RemoveFavorite(Token(context), playerId))));

        app.MapGet("/api/admin/users", (HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.GetManagedUsers(Token(context)))));

        app.MapPost("/api/admin/users", (CreateManagedUserRequestDto request, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.CreateManagedUser(Token(context), request))));

        app.MapDelete("/api/admin/users/{userId:int}", (int userId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.DeleteManagedUser(Token(context), userId))));

        app.MapPost("/api/admin/users/{userId:int}/role", (int userId, RoleChangeRequest request, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.UpdateUserRole(Token(context), userId, request.Role))));

        app.MapPost("/api/admin/users/{userId:int}/block", (int userId, BlockUserRequest request, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.SetUserBlocked(Token(context), userId, request.IsBlocked))));

        app.MapPost("/api/manager/players", (PlayerDto player, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.SavePlayer(Token(context), player))));

        app.MapDelete("/api/manager/players/{playerId:int}", (int playerId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.DeletePlayer(Token(context), playerId))));

        app.MapPost("/api/manager/teams", (TeamDto team, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.SaveTeam(Token(context), team))));

        app.MapDelete("/api/manager/teams/{teamId:int}", (int teamId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.DeleteTeam(Token(context), teamId))));

        app.MapPost("/api/manager/teams/{teamId:int}/players/{playerId:int}", (int teamId, int playerId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.AddPlayerToTeam(Token(context), teamId, playerId))));

        app.MapDelete("/api/manager/teams/{teamId:int}/players/{playerId:int}", (int teamId, int playerId, HttpContext context, IServiceProvider services) =>
            Results.Ok(WithService(services, service => service.RemovePlayerFromTeam(Token(context), teamId, playerId))));
    }

    /// <summary>מבצעת את שלב המערכת With Service באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Map Football Manager Web Api.</summary>
    /// <param name="services">ערך מסוג IServiceProvider שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="action">ערך מסוג Func&lt;FootballManagerService, T&gt; שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>ערך מסוג T שמשמש את הקוד הקורא.</returns>
    private static T WithService<T>(IServiceProvider services, Func<FootballManagerService, T> action)
    {
        using var scope = services.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<FootballManagerService>());
    }

    /// <summary>מבצעת את שלב המערכת Token באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Map Football Manager Web Api.</summary>
    /// <param name="context">ערך מסוג HttpContext שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <returns>מחרוזת שמייצגת את תוצאת הפעולה.</returns>
    private static string Token(HttpContext context) => context.Request.Cookies[SessionCookie] ?? string.Empty;

    /// <summary>מבצעת את שלב המערכת Public Session באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Map Football Manager Web Api.</summary>
    /// <param name="result">אובייקט נתונים שמעביר מידע בין שכבות.</param>
    /// <returns>אובייקט תשובה או נתונים לשכבה הקוראת.</returns>
    private static LoginResponseDto PublicSession(LoginResponseDto result) => new()
    {
        Succeeded = result.Succeeded,
        Message = result.Message,
        Username = result.Username,
        Role = result.Role
    };

    /// <summary>המחלקה RoleChangeRequest מרכזת אחריות של נקודות REST של האתר הקיים מ-C2. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
    public sealed record RoleChangeRequest(string Role);
    /// <summary>המחלקה BlockUserRequest מרכזת אחריות של נקודות REST של האתר הקיים מ-C2. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
    public sealed record BlockUserRequest(bool IsBlocked);
}
