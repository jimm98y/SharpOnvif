using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;

namespace SharpOnvifClient.Security
{
    [Flags]
    public enum OnvifAuthentication
    {
        None = 0,
        WsUsernameToken = 1,
        HttpDigest = 2
    }

    public static class OnvifAuthenticationExtensions
    {
        public static TChannel SetOnvifAuthentication<TChannel>(
            this ClientBase<TChannel> channel,
            NetworkCredential credentials,
            DigestAuthenticationSchemeOptions authentication,
            System.ServiceModel.Description.IEndpointBehavior legacyAuth) where TChannel : class
        {
            return SetOnvifAuthentication(channel as TChannel, credentials, authentication, legacyAuth);
        }

        public static TChannel SetOnvifAuthentication<TChannel>(
            TChannel wcfChannel,
            NetworkCredential credentials,
            DigestAuthenticationSchemeOptions authentication, 
            System.ServiceModel.Description.IEndpointBehavior legacyAuth) where TChannel : class
        {
            if (authentication.Authentication == OnvifAuthentication.None)
            {
                Debug.WriteLine("Authentication is disabled");
                return wcfChannel;
            }

            var channel = wcfChannel as ClientBase<TChannel>;
            if (channel == null)
                throw new ArgumentException($"{wcfChannel} is not WCF {nameof(ClientBase<TChannel>)}");

            if (authentication.Authentication.HasFlag(OnvifAuthentication.WsUsernameToken))
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
            if (authentication.Authentication.HasFlag(OnvifAuthentication.HttpDigest))
            {
                var state = new HttpDigestState();
                var proxy = HttpDigestProxy<TChannel>.CreateProxy(wcfChannel, state);

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
