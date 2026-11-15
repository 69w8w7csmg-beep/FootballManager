using System.Collections.Concurrent;

namespace FootballManager.WCF.Services;

public sealed class LoginSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

    public string Create(int userId)
    {
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        _sessions[token] = new SessionInfo(userId, DateTimeOffset.UtcNow.Add(SessionLifetime));
        return token;
    }

    public int? FindUserId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(token, out var session))
        {
            return null;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return session.UserId;
    }

    public void Remove(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _sessions.TryRemove(token, out _);
        }
    }

    private sealed record SessionInfo(int UserId, DateTimeOffset ExpiresAt);
}
