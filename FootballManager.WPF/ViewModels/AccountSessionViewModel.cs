using FootballManager.Contracts;

namespace FootballManager.WPF.ViewModels;

public sealed class AccountSessionViewModel
{
    public string SessionToken { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;

    public void Start(LoginResponseDto login)
    {
        SessionToken = login.SessionToken;
        Username = login.Username;
        Role = login.Role;
    }

    public void Clear()
    {
        SessionToken = string.Empty;
        Username = string.Empty;
        Role = string.Empty;
    }
}
