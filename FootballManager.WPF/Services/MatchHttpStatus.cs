// מטרה: שומר את קוד ה־HTTP של תשובת WCF במתאם WPF.
// מי משתמש: תשתית התקשורת של לקוח WPF.
// פעולות: התקנת ResponseHandler וקריאת Status.
// תלויות: HttpMessageHandler ו־IEndpointBehavior.
// קורא את קוד תשובת HTTP כדי להציג בלקוח את החסימות 401 ו־403 מהשרת.
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace FootballManager.WPF.Services;

// BasicHttpBinding converts both 401 and 403 into the same MessageSecurityException.
// Retain the actual HTTP status for this service call so the view can explain the denial.
internal sealed class MatchHttpStatus : IEndpointBehavior
{
    public HttpStatusCode? Status { get; private set; }

    /// <summary>רושם מאזין לתשובת HTTP כדי לשמור את קוד החסימה לפני שהלקוח ממיר אותה לחריגה.</summary>
    /// <param name="endpoint">ה־Endpoint שהלקוח מחבר אליו את ההתנהגות.</param>
    /// <param name="parameters">אוסף הגדרות ההעברה של WCF.</param>
    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection parameters)
    {
        parameters.Add(new Func<HttpClientHandler, HttpMessageHandler>(
            transport => new ResponseHandler(transport, this)));
    }

    /// <summary>ממלא את חוזה IEndpointBehavior. ההאזנה עצמה נרשמת ב־AddBindingParameters.</summary>
    /// <param name="endpoint">ה־Endpoint שהלקוח מחבר אליו את ההתנהגות.</param>
    /// <param name="runtime">סביבת ההפעלה של צד הלקוח.</param>
    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime runtime) { }
    /// <summary>ממלא את חוזה IEndpointBehavior בלי לשנות התנהגות בצד השירות.</summary>
    /// <param name="endpoint">ה־Endpoint שהלקוח מחבר אליו את ההתנהגות.</param>
    /// <param name="dispatcher">רכיב ניתוב בצד השירות.</param>
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher) { }
    /// <summary>ממלא את חוזה IEndpointBehavior בלי להוסיף כללי אימות ל־Endpoint.</summary>
    /// <param name="endpoint">ה־Endpoint שהלקוח מחבר אליו את ההתנהגות.</param>
    public void Validate(ServiceEndpoint endpoint) { }

    private sealed class ResponseHandler(HttpMessageHandler transport, MatchHttpStatus result)
        : DelegatingHandler(transport)
    {
        /// <summary>מעביר את בקשת HTTP ושומר את StatusCode של התשובה לפני החזרתה ל־WCF.</summary>
        /// <param name="request">בקשת HTTP או DTO לפי הפעולה.</param>
        /// <param name="token">CancellationToken שמאפשר לבטל HTTP. הוא אינו אסימון התחברות.</param>
        /// <returns>תשובת HTTP המקורית של השרת.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var response = await base.SendAsync(request, token).ConfigureAwait(false);
            result.Status = response.StatusCode;
            return response;
        }
    }
}
