using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;

namespace SharpOnvifClient
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
        public static void SetOnvifAuthentication<TChannel>(
            this ClientBase<TChannel> channel,
            OnvifAuthentication authentication, 
            NetworkCredential credentials, 
            System.ServiceModel.Description.IEndpointBehavior digestAuth,
            System.ServiceModel.Description.IEndpointBehavior legacyAuth) where TChannel : class
        {
            if (authentication == OnvifAuthentication.None)
            {
                Debug.WriteLine("Authentication is disabled");
                return;
            }

            if (authentication.HasFlag(OnvifAuthentication.HttpDigest))
            {
                // HTTP Digest authentication in NET Framework supports only MD5 (RFC 2069)
                if (digestAuth == null)
                    throw new ArgumentNullException(nameof(digestAuth));

                if (!channel.Endpoint.EndpointBehaviors.Contains(digestAuth))
                {
                    channel.Endpoint.EndpointBehaviors.Add(digestAuth);
                }
            }

            if (authentication.HasFlag(OnvifAuthentication.WsUsernameToken))
            {
                if(legacyAuth == null)
                    throw new ArgumentNullException(nameof(legacyAuth));    

                // legacy WsUsernameToken authentication must be handled using a custom behavior
                if (!channel.Endpoint.EndpointBehaviors.Contains(legacyAuth))
                {
                    channel.Endpoint.EndpointBehaviors.Add(legacyAuth);
                }
            }
        }
    }
}
