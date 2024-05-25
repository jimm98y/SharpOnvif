using CoreWCF;
using CoreWCF.Channels;
using System.Net;

namespace SharpOnvifServer
{
    /// <summary>
    /// Helper class for the CoreWCF bindings.
    /// </summary>
    public static class OnvifBindingFactory
    {
        /// <summary>
        /// Create an Onvif CoreWCF binding for the Digest authentication using Soap 1.2.
        /// </summary>
        /// <returns><see cref="CustomBinding"/> using Digest authentication and Soap 1.2.</returns>
        public static CustomBinding CreateBinding()
        {
            var httpTransportBinding = new HttpTransportBindingElement
            {
                AuthenticationScheme = AuthenticationSchemes.Digest,
            };
            var textMessageEncodingBinding = new TextMessageEncodingBindingElement
            {
                MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None),
            };
            var binding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
            return binding;
        }
    }
}
