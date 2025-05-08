using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
#if NETSTANDARD
using System.Reflection;
#endif

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
#if NETSTANDARD
                // Workaround for netstandard2.0 where when used in NETFramework 4.8.1 the AllowedImpersonationLevel property is set to Identification instead of Impersonation.
                try
                {
                    var allowedImpersonationLevelProperty = channel.ClientCredentials.HttpDigest.GetType().GetTypeInfo().GetProperty("AllowedImpersonationLevel", BindingFlags.Instance | BindingFlags.Public);
                    if (allowedImpersonationLevelProperty != null)
                    {
                        // Due to the limitations of Digest authentication, when the client is using non-default credentials, only Impersonation and Delegation levels are allowed.
                        allowedImpersonationLevelProperty.SetValue(channel.ClientCredentials.HttpDigest, System.Security.Principal.TokenImpersonationLevel.Impersonation);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error setting impersonation level: {ex.Message}");
                }
#elif NETFRAMEWORK
                channel.ClientCredentials.HttpDigest.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
#endif

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
