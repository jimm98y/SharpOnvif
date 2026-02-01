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
