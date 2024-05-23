using CoreWCF;
using CoreWCF.Channels;
using System.Net;

namespace SharpOnvifServer
{
    public static class OnvifHelpers
    {
        public static CustomBinding CreateOnvifBinding()
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
