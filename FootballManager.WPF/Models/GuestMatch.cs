// משחקי הדגמה לאורח במסך C2. שמות הקבוצות זהים לשמות שבמסד ההגשה.
namespace FootballManager.WPF.Models;

/// <summary>
/// תצוגת הדגמה ציבורית שהיתה קיימת כבר ב־C2.
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
            League = "ליגת העל",
            HomeTeam = "מכבי נתניה",
            AwayTeam = "הפועל חיפה",
            ScoreOrTime = "2 : 1",
            StatusLabel = "הסתיים",
            MatchDate = "03.11.2026",
            IsFinished = true
        },
        new()
        {
            League = "ליגת העל",
            HomeTeam = "הפועל חיפה",
            AwayTeam = "בני סכנין",
            ScoreOrTime = "1 : 1",
            StatusLabel = "הסתיים",
            MatchDate = "07.11.2026",
            IsFinished = true
        },
        new()
        {
            League = "ליגת העל",
            HomeTeam = "בני סכנין",
            AwayTeam = "מכבי נתניה",
            ScoreOrTime = "0 : 2",
            StatusLabel = "הסתיים",
            MatchDate = "10.11.2026",
            IsFinished = true
        },
        new()
        {
            League = "ליגת העל",
            HomeTeam = "מכבי נתניה",
            AwayTeam = "בני סכנין",
            ScoreOrTime = "20:30",
            StatusLabel = "בקרוב",
            MatchDate = "26.11.2026 · 20:30",
            IsFinished = false
        }
    ];
}
