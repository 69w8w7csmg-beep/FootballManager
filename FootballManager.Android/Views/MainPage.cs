// מטרה: מציג את חשבון המשתמש ואת תפריט Android בשלב 9.
// מי משתמש: App ומי שנכנס ליישום מ־Android.
// פעולות: ShowMenu, ShowAccount, LoginAsync, RegisterAsync, LogoutAsync.
// תלויות: WcfClient, DTO של החשבון והמסכים ב־MainPage.Content.
using System.Net;
using System.Runtime.Serialization;
using System.Xml;
using FootballManager.AndroidClient.Services;
using FootballManager.Contracts;

namespace FootballManager.AndroidClient.Views;

public sealed partial class MainPage : ContentPage
{
    private static readonly Color Ink = Color.FromArgb("#0C1724");
    private static readonly Color Panel = Color.FromArgb("#1C2C40");
    private static readonly Color Amber = Color.FromArgb("#F4C842");
    private static readonly Color Light = Color.FromArgb("#F0F5FA");
    private readonly WcfClient _service;
    private readonly VerticalStackLayout _screen = new() { Spacing = 12, Padding = new Thickness(22, 16) };
    private readonly Label _account = new() { TextColor = Light };
    private readonly Label _notice = new() { TextColor = Light, FontSize = 15, Margin = new Thickness(0, 5) };
    private string _token = "";
    private string _role = "Guest";
    private string _username = "";
    private bool _busy;

    /// <summary>בונה מעטפת מסך ואת התפריט הראשוני לאורח.</summary>
    /// <param name="service">מתאם חוזה WCF של Android.</param>
    public MainPage(WcfClient service)
    {
        _service = service;
        FlowDirection = FlowDirection.RightToLeft;
        BackgroundColor = Ink;
        Content = new ScrollView { Content = _screen };
        ShowMenu();
    }

    /// <summary>מרענן את כותרת המסך ואת שם החשבון, ומנקה הודעה ממסך קודם.</summary>
    /// <param name="title">טקסט הכותרת או הכפתור.</param>
    /// <param name="showBack">האם להציג כפתור חזרה לתפריט הראשי.</param>
    private void StartScreen(string title, bool showBack = true)
    {
        _screen.Children.Clear();
        var brand = Text("FOOTBALL MANAGER", 15, Amber);
        brand.FontAttributes = FontAttributes.Bold;
        brand.FlowDirection = FlowDirection.LeftToRight;
        brand.HorizontalOptions = LayoutOptions.End;
        _screen.Children.Add(brand);
        _account.Text = _role == "Guest" ? "מצב אורח" : $"{_username} · {RoleLabel(_role)}";
        _screen.Children.Add(_account);
        if (showBack) _screen.Children.Add(Button("חזרה לתפריט", ShowMenu));
        var heading = Text(title, 28);
        heading.FontAttributes = FontAttributes.Bold;
        _screen.Children.Add(heading);
        _notice.Text = "";
        _screen.Children.Add(_notice);
    }

    /// <summary>מציג את מדורי התוכן ואת פקודות החשבון לפי התפקיד שקיבל מהשרת.</summary>
    private void ShowMenu()
    {
        StartScreen("תפריט ראשי", showBack: false);
        if (_role == "Guest")
        {
            _screen.Children.Add(Button("כניסה לחשבון", () => ShowAccount(false), primary: true));
            _screen.Children.Add(Button("הרשמה כשחקן", () => ShowAccount(true)));
        }
        else
        {
            _screen.Children.Add(AsyncButton("התנתקות", LogoutAsync));
        }
        _screen.Children.Add(AsyncButton("שחקנים", () => ShowPlayersAsync(false)));
        _screen.Children.Add(AsyncButton("קבוצות וסגלים", ShowTeamsAsync));
        _screen.Children.Add(AsyncButton("משחקים ותוצאות", ShowMatchesAsync));
        if (_role == "Player") _screen.Children.Add(AsyncButton("המועדפים שלי", () => ShowPlayersAsync(true)));
        if (_role == "Admin") _screen.Children.Add(AsyncButton("ניהול משתמשים", ShowUsersAsync));
    }

    /// <summary>פותח טופס כניסה או הרשמה ומפעיל שליחה אחרי שהקלט תקין.</summary>
    /// <param name="registering">האם המסך מציג הרשמה במקום כניסה.</param>
    private void ShowAccount(bool registering)
    {
        StartScreen(registering ? "הרשמה כשחקן" : "כניסה לחשבון");
        var username = Field("שם משתמש");
        var password = Field("סיסמה", secret: true);
        username.MaxLength = 30;
        password.MaxLength = 128;
        Entry? confirmation = registering ? Field("אימות סיסמה", secret: true) : null;
        var submit = AsyncButton(registering ? "יצירת חשבון" : "התחברות",
            () => registering ? RegisterAsync(username.Text.Trim(), password.Text) : LoginAsync(username.Text.Trim(), password.Text),
            primary: true);
        // הכפתור תלוי בקלט הנוכחי. השרת בודק שוב כדי שכל הלקוחות כפופים לאותם כללים.
        // האירוע מגיע משדה שהשתנה. הבדיקה קוראת את הטופס הנוכחי ומעדכנת את כפתור השליחה.
        void Validate(object? sender, TextChangedEventArgs args)
        {
            var name = username.Text?.Trim() ?? "";
            var pass = password.Text ?? "";
            submit.IsEnabled = ValidUsername(name) && pass.Length is >= 8 and <= 128 &&
                (!registering || confirmation!.Text == pass);
        }
        username.TextChanged += Validate;
        password.TextChanged += Validate;
        if (confirmation != null) confirmation.TextChanged += Validate;
        submit.IsEnabled = false;
        _screen.Children.Add(submit);
    }

    /// <summary>מאמת חשבון ב־WCF ושומר את האסימון ואת התפקיד שהשרת החזיר בזיכרון המסך.</summary>
    /// <param name="username">שם החשבון מהטופס.</param>
    /// <param name="password">הסיסמה שהוקלדה בטופס.</param>
    private async Task LoginAsync(string username, string password)
    {
        var result = await _service.CallAsync<LoginResponseDto>("Login",
            ("request", new LoginRequestDto { Username = username, Password = password }));
        if (result.Succeeded)
        {
            _token = result.SessionToken;
            _role = result.Role;
            _username = result.Username;
            ShowMenu();
        }
        SetNotice(result.Message, !result.Succeeded);
    }

    /// <summary>שולח הרשמה ציבורית. השרת יוצר חשבון Player ומחזיר הצלחה או הודעת שגיאה.</summary>
    /// <param name="username">שם החשבון מהטופס.</param>
    /// <param name="password">הסיסמה שהוקלדה בטופס.</param>
    private async Task RegisterAsync(string username, string password)
    {
        var result = await _service.CallAsync<OperationResultDto>("Register",
            ("request", new RegisterRequestDto { Username = username, Password = password }));
        if (result.Succeeded) ShowAccount(false);
        SetNotice(result.Message, !result.Succeeded);
    }

    /// <summary>מבטל אסימון דרך WCF ומחזיר את המסך למצב אורח גם אם השרת אינו זמין.</summary>
    private async Task LogoutAsync()
    {
        try { await _service.CallAsync<OperationResultDto>("Logout", ("sessionToken", _token)); }
        finally
        {
            _token = "";
            _username = "";
            _role = "Guest";
            ShowMenu();
        }
    }

    /// <summary>מריץ פעולה אחת בכל פעם, ומציג על המסך את קוד הדחייה שהשרת החזיר.</summary>
    /// <param name="action">הפעולה שמפעילים כשהמשתמש לוחץ.</param>
    private async Task RunAsync(Func<Task> action)
    {
        if (_busy) return;
        _busy = true;
        try { await action(); }
        catch (WcfRequestException error)
        {
            // Status הוא מתשובת HTTP של WCF. התפריט המקומי משמש להצגת מסכים בלבד.
            SetNotice(error.Status switch
            {
                HttpStatusCode.Unauthorized => "401 Unauthorized · יש להתחבר לחשבון.",
                HttpStatusCode.Forbidden => "403 Forbidden · אין הרשאה לפעולה זו.",
                _ => $"שגיאה מהשרת ({(int)error.Status})."
            }, true);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or XmlException or SerializationException)
        {
            SetNotice("לא התקבלה תשובה תקינה משרת WCF. בדוק שהשרת וחיבור ה־USB פעילים.", true);
        }
        finally { _busy = false; }
    }

    /// <summary>מציג הודעה בשטח קבוע במסך בלי לפתוח חלון חדש.</summary>
    /// <param name="message">ההודעה שמוצגת למשתמש.</param>
    /// <param name="error">האם להדגיש את ההודעה כשגיאה.</param>
    private void SetNotice(string message, bool error = false)
    {
        _notice.Text = message;
        _notice.TextColor = error ? Color.FromArgb("#FFD0C9") : Color.FromArgb("#ABD9EE");
    }

    /// <summary>יוצר כפתור לפעולת תצוגה מקומית.</summary>
    /// <param name="title">טקסט הכותרת או הכפתור.</param>
    /// <param name="action">הפעולה שמפעילים כשהמשתמש לוחץ.</param>
    /// <param name="primary">האם להשתמש בצבע של כפתור הפעולה העיקרית.</param>
    private Button Button(string title, Action action, bool primary = false)
    {
        var button = StyledButton(title, primary);
        button.Clicked += (_, _) => { if (!_busy) action(); };
        return button;
    }

    /// <summary>יוצר כפתור שמפעיל בקשה אסינכרונית דרך RunAsync.</summary>
    /// <param name="title">טקסט הכותרת או הכפתור.</param>
    /// <param name="action">הפעולה שמפעילים כשהמשתמש לוחץ.</param>
    /// <param name="primary">האם להשתמש בצבע של כפתור הפעולה העיקרית.</param>
    private Button AsyncButton(string title, Func<Task> action, bool primary = false)
    {
        var button = StyledButton(title, primary);
        button.Clicked += async (_, _) => await RunAsync(action);
        return button;
    }

    /// <summary>בונה כפתור בסגנון הזהה לשאר המסך.</summary>
    /// <param name="title">טקסט הכותרת או הכפתור.</param>
    /// <param name="primary">האם להשתמש בצבע של כפתור הפעולה העיקרית.</param>
    private static Button StyledButton(string title, bool primary) => new()
    {
        Text = title, BackgroundColor = primary ? Amber : Panel, TextColor = primary ? Ink : Light,
        CornerRadius = 10, MinimumHeightRequest = 46, FontSize = 15
    };

    /// <summary>יוצר שדה עריכה עם תווית גלויה ומקלדת מספרית כשנדרשת.</summary>
    /// <param name="label">שם השדה שמוצג ליד הפקד.</param>
    /// <param name="value">הערך הראשוני לתצוגה.</param>
    /// <param name="number">האם לפתוח מקלדת מספרית.</param>
    /// <param name="secret">האם להסתיר את התווים בזמן ההקלדה.</param>
    private Entry Field(string label, string? value = null, bool number = false, bool secret = false)
    {
        _screen.Children.Add(Text(label, 14));
        var entry = new Entry { Text = value, Placeholder = label, IsPassword = secret, TextColor = Light,
            PlaceholderColor = Color.FromArgb("#9DB1C6"), BackgroundColor = Panel,
            Keyboard = number ? Keyboard.Numeric : Keyboard.Text, MinimumHeightRequest = 48 };
        _screen.Children.Add(entry);
        return entry;
    }

    /// <summary>יוצר בורר מתוך רשימה שהגיעה מהשרת או מרשימת התפקידים הקבועה.</summary>
    /// <param name="label">שם השדה שמוצג ליד הפקד.</param>
    /// <param name="rows">רשימת הערכים בתפריט הבחירה.</param>
    /// <param name="selected">אינדקס הבחירה הראשונית, או ‎-1 בלי בחירה.</param>
    private Picker Choice<T>(string label, IList<T> rows, int selected = -1)
    {
        _screen.Children.Add(Text(label, 14));
        var picker = new Picker { Title = label, ItemsSource = (System.Collections.IList)rows, TextColor = Light,
            TitleColor = Light, BackgroundColor = Panel, SelectedIndex = selected };
        _screen.Children.Add(picker);
        return picker;
    }

    /// <summary>מכין שורת טקסט עטופה לפי סגנון התצוגה.</summary>
    /// <param name="text">הטקסט לעיבוד או להצגה.</param>
    /// <param name="size">גודל הגופן.</param>
    /// <param name="color">צבע מותאם, או null לשימוש בצבע הרגיל.</param>
    private static Label Text(string text, double size, Color? color = null) =>
        new() { Text = text, FontSize = size, TextColor = color ?? Light, LineBreakMode = LineBreakMode.WordWrap };

    /// <summary>בודק מקומית את אורך שם המשתמש ואת התווים שמותר להקליד.</summary>
    /// <param name="name">שם המשתמש מהטופס.</param>
    private static bool ValidUsername(string name) =>
        name.Length is >= 3 and <= 30 && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    /// <summary>ממפה שם תפקיד מהשרת לכותרת שמופיעה בעברית.</summary>
    /// <param name="role">שם התפקיד שהשרת החזיר.</param>
    private static string RoleLabel(string role) => role switch
    {
        "Player" => "שחקן", "Manager" => "מאמן", "Admin" => "אדמין", _ => "אורח"
    };
}
