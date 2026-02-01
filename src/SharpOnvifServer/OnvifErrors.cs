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
using CoreWCF.Web;
using System.Net;

namespace SharpOnvifServer
{
    public static class OnvifErrors
    {
        /// <summary>
        /// Returns 400 Bad Request with InvalidArgVal error code.
        /// </summary>
        /// <exception cref="FaultException"></exception>
        public static void ReturnSenderInvalidArg()
        {
            ReturnSenderError("Argument Value Invalid", "InvalidArgVal");
        }

        /// <summary>
        /// Returns 400 Bad Request with ActionNotSupported error code.
        /// </summary>
        /// <exception cref="FaultException"></exception>
        public static void ReturnReceiverActionNotSupported()
        {
            ReturnReceiverError("Action Not Supported", "ActionNotSupported");
        }

        /// <summary>
        /// Returns receiver error.
        /// </summary>
        /// <exception cref="FaultException"></exception>
        public static void ReturnReceiverError(string reason, string subcodeName, string subcodeNamespace = "http://www.onvif.org/ver10/error", HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = httpStatusCode; // changes the default 500 status code
            throw new FaultException(new FaultReason(reason), FaultCode.CreateReceiverFaultCode(subcodeName, subcodeNamespace));
        }

        /// <summary>
        /// Returns sender error.
        /// </summary>
        /// <exception cref="FaultException"></exception>
        public static void ReturnSenderError(string reason, string subcodeName, string subcodeNamespace = "http://www.onvif.org/ver10/error", HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = httpStatusCode; // changes the default 500 status code
            throw new FaultException(new FaultReason(reason), FaultCode.CreateSenderFaultCode(subcodeName, subcodeNamespace));
        }
    }
}
