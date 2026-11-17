// נתוני העברות וסטטיסטיקות המשחק המשותפים ללקוח WPF ולשירות WCF.
using System.Runtime.Serialization;

namespace FootballManager.Contracts;

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

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class TransfersResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<TransferDto> Transfers { get; set; } = [];
}

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

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class PlayerStatisticsResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<PlayerStatisticDto> Statistics { get; set; } = [];
}
