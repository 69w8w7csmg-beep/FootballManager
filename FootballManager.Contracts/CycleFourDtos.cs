// תפקיד הקובץ: הגדרת DTO להעברת נתוני העברה וסטטיסטיקות.
// שכבה: חוזים משותפים לפרויקטי WPF ולשירות WCF.
// משתמשים: לקוח WPF, שירות WCF ומימושי החוזה.
// קשר בפרויקט: הטיפוסים כאן מתארים את המידע שעובר בין המסך, השירות והמסד.
using System.Runtime.Serialization;

namespace FootballManager.Contracts;

/// <summary>DTO שמייצג העברה בין קבוצות: מזהי שחקן וקבוצות, תאריך ומחיר. WPF שולח אותו בחוזה WCF; המאגר שומר את שדות ההעברה, ושמות התצוגה מתמלאים בשליפה.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class TransferDto
{
    [DataMember(Order = 1)] public int TransferId { get; set; }
    [DataMember(Order = 2)] public int PlayerId { get; set; }
    [DataMember(Order = 3)] public int FromTeamId { get; set; }
    [DataMember(Order = 4)] public int ToTeamId { get; set; }
    [DataMember(Order = 5)] public DateTime TransferDate { get; set; }
    [DataMember(Order = 6)] public decimal Price { get; set; }
    [DataMember(Order = 7)] public string PlayerName { get; set; } = string.Empty;
    [DataMember(Order = 8)] public string FromTeamName { get; set; } = string.Empty;
    [DataMember(Order = 9)] public string ToTeamName { get; set; } = string.Empty;
}

/// <summary>מעטפת תשובה של שירות C4. היא מחזירה אם שליפת ההעברות הצליחה, הודעת מצב ורשימת TransferDto.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class TransfersResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<TransferDto> Transfers { get; set; } = [];
}

/// <summary>DTO של נתוני שחקן במשחק אחד: מזהי שחקן ומשחק, שערים, בישולים, דקות ודירוג. עובר בין WPF לשירות וממופה לטבלת PlayerStatistics.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class PlayerStatisticDto
{
    [DataMember(Order = 1)] public int StatisticId { get; set; }
    [DataMember(Order = 2)] public int PlayerId { get; set; }
    [DataMember(Order = 3)] public int MatchId { get; set; }
    [DataMember(Order = 4)] public int Goals { get; set; }
    [DataMember(Order = 5)] public int Assists { get; set; }
    [DataMember(Order = 6)] public int MinutesPlayed { get; set; }
    [DataMember(Order = 7)] public float Rating { get; set; }
    [DataMember(Order = 8)] public string PlayerName { get; set; } = string.Empty;
    [DataMember(Order = 9)] public string MatchLabel { get; set; } = string.Empty;
}

/// <summary>מעטפת תשובה של שירות C4 עבור שליפת סטטיסטיקות משחק, עם הודעת שירות ורשימת PlayerStatisticDto.</summary>
[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class PlayerStatisticsResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<PlayerStatisticDto> Statistics { get; set; } = [];
}
