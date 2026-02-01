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

using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;

namespace SharpOnvifClient.Security
{
    public static class OnvifAuthenticationExtensions
    {
        public static TChannel SetOnvifAuthentication<TChannel>(
            TChannel wcfChannel,
            NetworkCredential credentials,
            DigestAuthenticationSchemeOptions authentication, 
            System.ServiceModel.Description.IEndpointBehavior legacyAuth) where TChannel : class
        {
            if (authentication.Authentication == DigestAuthentication.None)
            {
                Debug.WriteLine("Authentication is disabled");
                return wcfChannel;
            }

            var channel = wcfChannel as ClientBase<TChannel>;
            if (channel == null)
                throw new ArgumentException($"{wcfChannel} is not WCF {nameof(ClientBase<TChannel>)}");

            if (authentication.Authentication.HasFlag(DigestAuthentication.WsUsernameToken))
            {
                if (legacyAuth == null)
                    throw new ArgumentNullException(nameof(legacyAuth));

                // legacy WsUsernameToken authentication must be handled using a custom behavior
                if (!channel.Endpoint.EndpointBehaviors.Contains(legacyAuth))
                {
                    channel.Endpoint.EndpointBehaviors.Add(legacyAuth);
                }
            }

            // we need HttpDigest last in the behavior pipeline so that it has the final request to calculate the hash
            if (authentication.Authentication.HasFlag(DigestAuthentication.HttpDigest))
            {
                var state = new HttpDigestState();
                var proxy = HttpDigestProxy<TChannel>.CreateProxy(wcfChannel, state);
                var digestAuth = new HttpDigestBehavior(credentials, authentication, state);

                if (!channel.Endpoint.EndpointBehaviors.Contains(digestAuth))
                {
                    channel.Endpoint.EndpointBehaviors.Add(digestAuth);
                }

                return proxy;
            }
            else
            {
                return wcfChannel;
            }
        }
    }
}
