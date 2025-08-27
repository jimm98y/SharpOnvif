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
        /// The default message size is 65536 which can be too small nowdays - increase it to 1 MB.
        /// </summary>
        private const int MAX_MESSAGE_SIZE = 1048576;

        /// <summary>
        /// Create an Onvif CoreWCF binding for the Digest authentication using Soap 1.2.
        /// </summary>
        /// <returns><see cref="CustomBinding"/> using Digest authentication and Soap 1.2.</returns>
        public static CustomBinding CreateBinding()
        {
            var httpTransportBinding = new HttpTransportBindingElement
            {
                AuthenticationScheme = AuthenticationSchemes.Digest,
                MaxReceivedMessageSize = MAX_MESSAGE_SIZE,
                MaxBufferSize = MAX_MESSAGE_SIZE
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
