// נתוני המשחק ותשובת השליפה המשותפים ללקוח WPF ולשירות WCF.
using System.Runtime.Serialization;

namespace FootballManager.Contracts;

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class MatchDto
{
    [DataMember(Order = 1)] public int MatchId { get; set; }
    [DataMember(Order = 2)] public int HomeTeamId { get; set; }
    [DataMember(Order = 3)] public int AwayTeamId { get; set; }
    [DataMember(Order = 4)] public DateTime MatchDate { get; set; }
    [DataMember(Order = 5)] public int HomeScore { get; set; }
    [DataMember(Order = 6)] public int AwayScore { get; set; }
    [DataMember(Order = 7)] public string HomeTeamName { get; set; } = string.Empty;
    [DataMember(Order = 8)] public string AwayTeamName { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class MatchesResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<MatchDto> Matches { get; set; } = [];
}
