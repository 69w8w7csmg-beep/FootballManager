// סינון משחקי ההדגמה הציבוריים של C2 לפי מצב המשחק.
using FootballManager.WPF.Models;

namespace FootballManager.WPF.ViewModels;

public sealed class GuestMatchesViewModel
{
    private readonly IReadOnlyList<GuestMatch> _fixtures = GuestMatch.DemoFixtures;

    public IReadOnlyList<GuestMatch> Filter(string filter) => filter switch
    {
        "Finished" => _fixtures.Where(match => match.IsFinished).ToList(),
        "Upcoming" => _fixtures.Where(match => !match.IsFinished).ToList(),
        _ => _fixtures.ToList()
    };
}
