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
            System.ServiceModel.Description.IEndpointBehavior legacyAuth = null) where TChannel : class
        {
            if (authentication == OnvifAuthentication.None)
            {
                Debug.WriteLine("Authentication is disabled");
                return;
            }

            if (authentication.HasFlag(OnvifAuthentication.HttpDigest))
            {
                // HTTP Digest authentication is handled by WCF
                channel.ClientCredentials.HttpDigest.ClientCredential = credentials;
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
