﻿using SharpOnvifClient.Security;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace SharpOnvifClient
{
    /// <summary>
    /// Helper class for the WCF auto-generated stubs.
    /// </summary>
    public static class OnvifHelper
    {
        public static Binding CreateBinding()
        {
            var httpTransportBinding = new HttpTransportBindingElement
            {
                AuthenticationScheme = AuthenticationSchemes.Digest
            };
            var textMessageEncodingBinding = new TextMessageEncodingBindingElement
            {
                MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
            };
            var customBinding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
            return customBinding;
        }

        public static IEndpointBehavior CreateAuthenticationBehavior(string userName, string password)
        {
            return new DigestBehavior(userName, password);
        }

        public static void SetAuthentication(ServiceEndpoint endpoint, IEndpointBehavior authenticationBehavior)
        {
            if (!endpoint.EndpointBehaviors.Contains(authenticationBehavior))
            {
                endpoint.EndpointBehaviors.Add(authenticationBehavior);
            }
        }

        public static EndpointAddress CreateEndpointAddress(string endPointAddress)
        {
            return new EndpointAddress(endPointAddress);
        }
    }
}
