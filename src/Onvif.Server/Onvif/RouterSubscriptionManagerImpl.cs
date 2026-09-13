// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using SharpOnvifServer;
using SharpOnvifServer.Dispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SharpOnvifServer.Events;

namespace OnvifService.Onvif
{
    /// <summary>
    /// Routes all incoming Subscription Manager requests to the corresponding Subscription Manager instances.
    /// </summary>
    public class RouterSubscriptionManagerImpl : SubscriptionManagerBase
    {
        private readonly ILogger<RouterSubscriptionManagerImpl> _logger;
        private readonly IEventSubscriptionManager<SubscriptionManagerImpl> _eventSubscriptionManager;

        public RouterSubscriptionManagerImpl(ILogger<RouterSubscriptionManagerImpl> logger, IEventSubscriptionManager<SubscriptionManagerImpl> eventSubscriptionManager)
        {
            _logger = logger;
            _eventSubscriptionManager = eventSubscriptionManager;
        }

        public override RenewResponse Renew(RenewRequest request)
        {
            return GetSubscriptionManager().Renew(request);
        }

        public override UnsubscribeResponse Unsubscribe(UnsubscribeRequest request)
        {
            var ret = GetSubscriptionManager().Unsubscribe(request);
            
            string subscriptionID = GetSubscriptionID();
            _eventSubscriptionManager.RemoveSubscription(subscriptionID);
            _logger.LogDebug($"{nameof(RouterSubscriptionManagerImpl)}: Unsubscribed {UntrustedText.Printable(subscriptionID)}");

            return ret;
        }

        private SubscriptionManagerImpl GetSubscriptionManager()
        {
            var subscription = _eventSubscriptionManager.GetSubscription(GetSubscriptionID());
            if (subscription == null)
                throw new OnvifServerFaultException("Sender", "InvalidArgVal", OnvifErrors.Namespace, $"Subscription {GetSubscriptionID()} does not exist.", System.Net.HttpStatusCode.BadRequest);
            return subscription;
        }

        /// <summary>
        /// Retrieves the Subscription ID from the <see cref="HttpContext"/>.
        /// </summary>
        /// <returns>Subscription ID of the current request.</returns>
        private static string GetSubscriptionID()
        {
            HttpContext httpContext = OnvifOperationContext.Current;
            return httpContext?.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID] as string;
        }
    }
}
