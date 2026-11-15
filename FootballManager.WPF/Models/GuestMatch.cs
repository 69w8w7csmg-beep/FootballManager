namespace FootballManager.WPF.Models;

/// <summary>
/// A clearly labeled sample fixture used until public match data is added to the database.
/// </summary>
public sealed class GuestMatch
{
    public string League { get; init; } = "";
    public string HomeTeam { get; init; } = "";
    public string AwayTeam { get; init; } = "";
    public string ScoreOrTime { get; init; } = "";
    public string StatusLabel { get; init; } = "";
    public string MatchDate { get; init; } = "";
    public bool IsFinished { get; init; }

    public static IReadOnlyList<GuestMatch> DemoFixtures =>
    [
        new()
        {
            League = "ליגת הדגמה",
            HomeTeam = "עירוני השרון",
            AwayTeam = "הפועל החוף",
            ScoreOrTime = "2 : 1",
            StatusLabel = "הסתיים",
            MatchDate = "יום ראשון · 18:30",
            IsFinished = true
        },
        new()
        {
            League = "ליגת הדגמה",
            HomeTeam = "מכבי כרמל",
            AwayTeam = "בית״ר העמק",
            ScoreOrTime = "1 : 1",
            StatusLabel = "הסתיים",
            MatchDate = "יום שבת · 20:00",
            IsFinished = true
        },
        new()
        {
            League = "ליגת הדגמה",
            HomeTeam = "הפועל הגבעה",
            AwayTeam = "עירוני הנגב",
            ScoreOrTime = "19:45",
            StatusLabel = "בקרוב",
            MatchDate = "יום שלישי · 19:45",
            IsFinished = false
        },
        new()
        {
            League = "ליגת הדגמה",
            HomeTeam = "מכבי המפרץ",
            AwayTeam = "הפועל הגליל",
            ScoreOrTime = "20:30",
            StatusLabel = "בקרוב",
            MatchDate = "יום חמישי · 20:30",
            IsFinished = false
        }
    ];
}
