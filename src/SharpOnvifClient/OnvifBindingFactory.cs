// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using System;
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
        public static Binding CreateBinding(string onvifUri)
        {
            if(string.IsNullOrEmpty(onvifUri))
                throw new ArgumentNullException($"{nameof(onvifUri)} cannot be null or empty");

            TransportBindingElement httpTransportBinding = null;
            if (onvifUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                httpTransportBinding = new HttpTransportBindingElement
                {
                    AuthenticationScheme = AuthenticationSchemes.Anonymous,
                    MaxReceivedMessageSize = MAX_MESSAGE_SIZE,
                    MaxBufferSize = MAX_MESSAGE_SIZE,
                };
            }
            else if(onvifUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                httpTransportBinding = new HttpsTransportBindingElement
                {
                    AuthenticationScheme = AuthenticationSchemes.Anonymous,
                    MaxReceivedMessageSize = MAX_MESSAGE_SIZE,
                    MaxBufferSize = MAX_MESSAGE_SIZE,
                };
            }
            else
            {
                throw new NotSupportedException(onvifUri);
            }

            var textMessageEncodingBinding = new TextMessageEncodingBindingElement
            {
                MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None)
            };
            var customBinding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
            return customBinding;
        }
    }
}
