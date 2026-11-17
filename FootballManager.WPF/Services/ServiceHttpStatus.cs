// תפקיד הקובץ: שמירת קוד HTTP המקורי כדי להציג 401 או 403 בלקוח.
// שכבה: תשתית התקשורת של WPF מול WCF.
// משתמשים: WcfServiceClient והמסכים שמציגים הודעת חסימת הרשאה.
// קשר בפרויקט: התנהגות זו שומרת את קוד התשובה מהשרת ואינה קובעת הרשאה.
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace FootballManager.WPF.Services;

// BasicHttpBinding מציג גם 401 וגם 403 כ־MessageSecurityException.
// המחלקה שומרת את קוד HTTP המקורי כדי שהמסך יוכל להסביר את סיבת החסימה.
/// <summary>רכיב תקשורת של WPF ששומר את קוד HTTP המקורי שמוחזר מ־WCF, כדי שהממשק יוכל להציג הבדל בין 401 ל־403.</summary>
internal sealed class ServiceHttpStatus : IEndpointBehavior
{
    public HttpStatusCode? Status { get; private set; }

    /// <summary>מבצעת את פעולת הנתונים Add Binding Parameters. בשרת נבדקים הנתונים וההרשאה לפי מימוש הפעולה.</summary>
    /// <param name="endpoint">ערך מסוג ServiceEndpoint שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="parameters">ערך מסוג BindingParameterCollection שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection parameters)
    {
        parameters.Add(new Func<HttpClientHandler, HttpMessageHandler>(
            transport => new ResponseHandler(transport, this)));
    }

    /// <summary>מבצעת את שלב המערכת Apply Client Behavior באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="endpoint">ערך מסוג ServiceEndpoint שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="runtime">ערך מסוג ClientRuntime שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime runtime) { }
    /// <summary>מבצעת את שלב המערכת Apply Dispatch Behavior באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה.</summary>
    /// <param name="endpoint">ערך מסוג ServiceEndpoint שהפעולה צריכה כדי לבצע את תפקידה.</param>
    /// <param name="dispatcher">ערך מסוג EndpointDispatcher שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher) { }
    /// <summary>בודקת את תקינות הנתונים עבור Validate ומחזירה את תוצאת הבדיקה כדי למנוע המשך עם קלט שגוי.</summary>
    /// <param name="endpoint">ערך מסוג ServiceEndpoint שהפעולה צריכה כדי לבצע את תפקידה.</param>
    public void Validate(ServiceEndpoint endpoint) { }

    /// <summary>המחלקה ResponseHandler מרכזת אחריות של שירותי הלקוח WPF. רכיבים אחרים משתמשים בה כדי לבצע את פעולותיה ולחבר בין שכבות הפרויקט.</summary>
    private sealed class ResponseHandler(HttpMessageHandler transport, ServiceHttpStatus result)
        : DelegatingHandler(transport)
    {
        /// <summary>מבצעת את שלב המערכת Send Async באמצעות הנתונים שבפרמטרים וממשיכה את זרימת העבודה של המחלקה. נקראת בקוד מתוך Send Async.</summary>
        /// <param name="request">ערך מסוג HttpRequestMessage שהפעולה צריכה כדי לבצע את תפקידה.</param>
        /// <param name="token">אסימון ההפעלה של המשתמש; השרת מזהה בעזרתו את המשתמש ואינו מסתמך על Role שנשלח מהלקוח.</param>
        /// <returns>משימה אסינכרונית שמסתיימת כשהפעולה והשירות שאליו פנתה מסתיימים.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var response = await base.SendAsync(request, token).ConfigureAwait(false);
            result.Status = response.StatusCode;
            return response;
        }
    }
}
