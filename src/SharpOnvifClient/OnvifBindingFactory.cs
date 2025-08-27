using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace SharpOnvifClient
{
    /// <summary>
    /// Helper class for the WCF auto-generated stubs.
    /// </summary>
    public static class OnvifBindingFactory
    {
        /// <summary>
        /// The default message size is 65536 which can be too small nowdays - increase it to 1 MB.
        /// </summary>
        private const int MAX_MESSAGE_SIZE = 1048576;

        /// <summary>
        /// Create an Onvif WCF binding for the Digest authentication using Soap 1.2.
        /// </summary>
        public static Binding CreateBinding()
        {
            var httpTransportBinding = new HttpTransportBindingElement
            {
                AuthenticationScheme = AuthenticationSchemes.Digest,
                MaxReceivedMessageSize = MAX_MESSAGE_SIZE,
                MaxBufferSize = MAX_MESSAGE_SIZE
            };
            var textMessageEncodingBinding = new TextMessageEncodingBindingElement
            {
                MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
            };
            var customBinding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
            return customBinding;
        }
    }
}
