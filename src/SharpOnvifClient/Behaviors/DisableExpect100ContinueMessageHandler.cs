using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOnvifClient.Behaviors
{
    public class DisableExpect100ContinueMessageHandler : DelegatingHandler
    {
        public DisableExpect100ContinueMessageHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;

            request.Headers.ExpectContinue = false;
            response = await base.SendAsync(request, cancellationToken);

            return response;
        }
    }
}
