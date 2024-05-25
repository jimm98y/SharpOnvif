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
    }
}
