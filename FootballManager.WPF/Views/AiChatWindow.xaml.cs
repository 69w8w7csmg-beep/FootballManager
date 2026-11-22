using System.Windows;
using FootballManager.Contracts;
using FootballManager.WPF.Services;
using FootballManager.WPF.ViewModels;

namespace FootballManager.WPF.Views;

/// <summary>חלון צ'אט WPF. הוא קורא לחוזה WCF בלבד ואינו פונה ל־Access או ל־Gemini.</summary>
public partial class AiChatWindow : Window
{
    private readonly WcfServiceClient _service;
    private readonly AccountSessionViewModel _session;

    public AiChatWindow(WcfServiceClient service, AccountSessionViewModel session)
    {
        InitializeComponent();
        _service = service;
        _session = session;
    }

    /// <summary>שולחת את השאלה והאסימון לשרת ומציגה תשובה או שגיאה בלי לסגור את החלון.</summary>
    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var question = QuestionBox.Text.Trim();
        if (question.Length < 3 || question.Length > 1000)
        {
            StatusText.Text = "יש להזין שאלה באורך 3 עד 1000 תווים.";
            return;
        }
        SendButton.IsEnabled = false;
        ResetButton.IsEnabled = false;
        StatusText.Text = "ממתין לתשובת השרת…";
        try
        {
            var result = await _service.ChatAsync(new AiChatRequestDto
            {
                SessionToken = _session.SessionToken, Question = question
            });
            StatusText.Text = result.Message;
            if (!result.Succeeded) return;
            if (TranscriptText.Text == "אפשר לשאול שאלה כדי להתחיל.") TranscriptText.Text = string.Empty;
            TranscriptText.Text += $"אני: {question}\n\nעוזר: {result.Answer}\n";
            if (result.AddedFavoritePlayerId > 0) TranscriptText.Text += "המועדף נשמר במסד.\n";
            TranscriptText.Text += "\n";
            QuestionBox.Clear();
            ChatScroll.ScrollToEnd();
        }
        catch (Exception)
        {
            StatusText.Text = "לא ניתן לקבל תשובה כרגע. בדוק שהשרת פועל ונסה שוב.";
        }
        finally { SendButton.IsEnabled = true; ResetButton.IsEnabled = true; }
    }

    /// <summary>מאפסת את השיחה בשרת עבור המשתמש המחובר ומנקה את התצוגה רק אחרי הצלחה.</summary>
    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        SendButton.IsEnabled = false;
        ResetButton.IsEnabled = false;
        try
        {
            var result = await _service.ChatAsync(new AiChatRequestDto
            {
                SessionToken = _session.SessionToken, ResetHistory = true
            });
            StatusText.Text = result.Message;
            if (result.Succeeded) TranscriptText.Text = "אפשר לשאול שאלה כדי להתחיל.";
        }
        catch (Exception) { StatusText.Text = "לא ניתן לאפס את השיחה כרגע."; }
        finally { SendButton.IsEnabled = true; ResetButton.IsEnabled = true; }
    }
}
