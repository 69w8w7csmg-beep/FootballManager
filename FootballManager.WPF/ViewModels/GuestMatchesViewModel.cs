// מטרה: מסנן תוצאות דוגמה למסך אורח ב־WPF.
// מי משתמש: MainWindow.
// פעולות: בחירת משחקים לפי מסנן מצב.
// תלויות: GuestMatch.DemoFixtures.
// סינון משחקי ההדגמה הציבוריים של C2 לפי מצב המשחק.
using FootballManager.WPF.Models;

namespace FootballManager.WPF.ViewModels;

/// <summary>מסנן בצד WPF את רשימת המשחקים הציבורית שכבר נטענה, לפי טקסט חיפוש.</summary>
public sealed class GuestMatchesViewModel
{
    private readonly IReadOnlyList<GuestMatch> _fixtures = GuestMatch.DemoFixtures;

    /// <summary>שולפת או מסננת את הנתונים המתוארים בשם Filter ומחזירה את התוצאה לשכבה הקוראת. נקראת בקוד מתוך Apply Match Filter.</summary>
    /// <param name="filter">טקסט שהפעולה משתמשת בו, כגון חיפוש, הודעה או ערך טופס.</param>
    /// <returns>רשימת הרשומות שנשלפו.</returns>
    public IReadOnlyList<GuestMatch> Filter(string filter) => filter switch
    {
        "Finished" => _fixtures.Where(match => match.IsFinished).ToList(),
        "Upcoming" => _fixtures.Where(match => !match.IsFinished).ToList(),
        _ => _fixtures.ToList()
    };
}
