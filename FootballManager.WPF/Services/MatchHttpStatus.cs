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

    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection parameters)
    {
        parameters.Add(new Func<HttpClientHandler, HttpMessageHandler>(
            transport => new ResponseHandler(transport, this)));
    }

    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime runtime) { }
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher) { }
    public void Validate(ServiceEndpoint endpoint) { }

    private sealed class ResponseHandler(HttpMessageHandler transport, MatchHttpStatus result)
        : DelegatingHandler(transport)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var response = await base.SendAsync(request, token).ConfigureAwait(false);
            result.Status = response.StatusCode;
            return response;
        }
    }
}
