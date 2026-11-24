// מטרה: בקשה ותשובה לצ׳אט WPF.
// מי משתמש: AiChatWindow ו־FootballManagerService.
// פעולות: AiChatRequestDto ו־AiChatResponseDto.
// תלויות: DataContract של WCF.
using System.Runtime.Serialization;

namespace FootballManager.Contracts;

/// <summary>בקשת צ'אט מהלקוח; זהות ותפקיד נגזרים מהאסימון בשרת.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class AiChatRequestDto
{
    [DataMember(Order = 1)] public string SessionToken { get; set; } = string.Empty;
    [DataMember(Order = 2)] public string Question { get; set; } = string.Empty;
    [DataMember(Order = 3)] public bool ResetHistory { get; set; }
}

/// <summary>תשובת הצ'אט והודעה על הוספת מועדף, אם בוצעה.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class AiChatResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Answer { get; set; } = string.Empty;
    [DataMember(Order = 4)] public int AddedFavoritePlayerId { get; set; }
}
