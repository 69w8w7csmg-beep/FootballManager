// תפקיד הקובץ: שומר תורי שיחה לפי מזהה משתמש.
// שכבה: שירותים בשרת WCF.
// משתמשים: שירות הצ'אט.
// קשר בפרויקט: מפריד את ההיסטוריה של כל חשבון ושומר אותה בזיכרון.
using System.Collections.Concurrent;

namespace FootballManager.WCF.Services;

/// <summary>שומר שיחות בזיכרון השרת לפי מזהה משתמש ומסדר בקשות מקבילות של אותו משתמש.</summary>
public sealed class AiChatHistoryStore
{
    private readonly ConcurrentDictionary<int, UserConversation> _conversations = new();

    public UserConversation ForUser(int userId) => _conversations.GetOrAdd(userId, _ => new UserConversation());

    public sealed class UserConversation
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public List<AiChatTurn> Turns { get; } = [];
    }
}
