using System.Runtime.Serialization;

namespace FootballManager.Contracts;

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class ServiceStatusDto
{
    [DataMember(Order = 1)]
    public string ApplicationName { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public bool DatabaseReady { get; set; }

    [DataMember(Order = 3)]
    public string Message { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class RegisterRequestDto
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Role { get; set; } = "Player";
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class CreateManagedUserRequestDto
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Role { get; set; } = "Player";
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class OperationResultDto
{
    [DataMember(Order = 1)]
    public bool Succeeded { get; set; }

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class LoginRequestDto
{
    [DataMember(Order = 1)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 2)]
    public string Password { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class LoginResponseDto
{
    [DataMember(Order = 1)]
    public bool Succeeded { get; set; }

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string SessionToken { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 5)]
    public string Role { get; set; } = string.Empty;
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class ManagedUserDto
{
    [DataMember(Order = 1)]
    public int UserId { get; set; }

    [DataMember(Order = 2)]
    public string Username { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public string Role { get; set; } = string.Empty;

    [DataMember(Order = 4)]
    public bool IsBlocked { get; set; }

    public override string ToString()
    {
        return $"{Username} | {Role} | {(IsBlocked ? "חסום" : "פעיל")}";
    }
}

[DataContract(Namespace = "urn:footballmanager:contracts")]
public sealed class ManagedUsersResponseDto
{
    [DataMember(Order = 1)]
    public bool Succeeded { get; set; }

    [DataMember(Order = 2)]
    public string Message { get; set; } = string.Empty;

    [DataMember(Order = 3)]
    public List<ManagedUserDto> Users { get; set; } = [];
}
