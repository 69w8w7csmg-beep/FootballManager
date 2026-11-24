// מטרה: מבודד תורי שיחה לפי חשבון בזיכרון השרת.
// מי משתמש: FootballManagerService.ChatAsync.
// פעולות: ForUser מאתר או יוצר תור לפי UserId.
// תלויות: ConcurrentDictionary ו־SemaphoreSlim.
using System.Collections.Concurrent;

namespace FootballManager.WCF.Services;

/// <summary>שומר שיחות בזיכרון השרת לפי מזהה משתמש ומסדר בקשות מקבילות של אותו משתמש.</summary>
public sealed class AiChatHistoryStore
{
    private readonly ConcurrentDictionary<int, UserConversation> _conversations = new();

    /// <summary>מחזיר את תור הצ׳אט השייך למזהה החשבון ויוצר תור ריק בפנייה הראשונה.</summary>
    /// <param name="userId">מזהה המשתמש שנשלף או מתעדכן.</param>
    /// <returns>מופע התור שמשויך למשתמש בזיכרון השרת.</returns>
    public UserConversation ForUser(int userId) => _conversations.GetOrAdd(userId, _ => new UserConversation());

    public sealed class UserConversation
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public List<AiChatTurn> Turns { get; } = [];
    }
}
