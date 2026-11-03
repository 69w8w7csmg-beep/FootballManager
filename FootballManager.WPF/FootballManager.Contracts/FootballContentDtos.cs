using System.Runtime.Serialization;

namespace FootballManager.Contracts;

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class PlayerDto
{
    [DataMember(Order = 1)] public int PlayerId { get; set; }
    [DataMember(Order = 2)] public string FirstName { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string LastName { get; set; } = string.Empty;
    [DataMember(Order = 4)] public int Age { get; set; }
    [DataMember(Order = 5)] public string Position { get; set; } = string.Empty;
    [DataMember(Order = 6)] public int Rating { get; set; }
    [DataMember(Order = 7)] public int Speed { get; set; }
    [DataMember(Order = 8)] public int Shooting { get; set; }
    [DataMember(Order = 9)] public int Passing { get; set; }
    [DataMember(Order = 10)] public int Defense { get; set; }

    public override string ToString() =>
        $"{FirstName} {LastName} · {Position} · דירוג {Rating} · גיל {Age}";
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class TeamDto
{
    [DataMember(Order = 1)] public int TeamId { get; set; }
    [DataMember(Order = 2)] public string Name { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Country { get; set; } = string.Empty;
    [DataMember(Order = 4)] public string League { get; set; } = string.Empty;
    [DataMember(Order = 5)] public decimal Budget { get; set; }

    public override string ToString() => $"{Name} · {Country} · {League}";
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class PlayersResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<PlayerDto> Players { get; set; } = [];
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class TeamsResponseDto
{
    [DataMember(Order = 1)] public bool Succeeded { get; set; }
    [DataMember(Order = 2)] public string Message { get; set; } = string.Empty;
    [DataMember(Order = 3)] public List<TeamDto> Teams { get; set; } = [];
}
