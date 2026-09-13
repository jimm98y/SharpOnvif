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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using SharpOnvifCommon.Xml;

namespace SharpOnvifServer
{
    /// <summary>
    /// A fault an Onvif service implementation raises deliberately. The endpoint turns it into a
    /// SOAP 1.2 fault with the requested code, subcode, and HTTP status.
    /// </summary>
    public class OnvifServerFaultException : SoapFaultException
    {
        public OnvifServerFaultException(
            string code, string subcode, string subcodeNamespace, string reason, HttpStatusCode statusCode)
            : base(BuildFault(code, subcode, reason))
        {
            SubcodeNamespace = subcodeNamespace;
            StatusCode = statusCode;
        }

        /// <summary>Namespace the subcode belongs to, normally the Onvif error namespace.</summary>
        public string SubcodeNamespace { get; private set; }

        /// <summary>HTTP status to answer with.</summary>
        public HttpStatusCode StatusCode { get; private set; }

        private static SoapFault BuildFault(string code, string subcode, string reason)
        {
            var fault = new SoapFault { Code = code, Reason = reason };
            fault.Subcodes.Add(subcode);
            return fault;
        }
    }

    /// <summary>Helpers for reporting the faults the Onvif specification defines.</summary>
    public static class OnvifErrors
    {
        /// <summary>The Onvif error namespace, conventionally bound to the "ter" prefix.</summary>
        public const string Namespace = "http://www.onvif.org/ver10/error";

        /// <summary>Reports that an argument was not acceptable.</summary>
        [DoesNotReturn]
        public static void ReturnSenderInvalidArg()
        {
            ReturnSenderError("Argument Value Invalid", "InvalidArgVal");
        }

        /// <summary>Reports that the device does not implement the requested operation.</summary>
        [DoesNotReturn]
        public static void ReturnReceiverActionNotSupported()
        {
            ReturnReceiverError("Action Not Supported", "ActionNotSupported");
        }

        /// <summary>Reports a fault caused by the device.</summary>
        [DoesNotReturn]
        public static void ReturnReceiverError(
            string reason,
            string subcodeName,
            string subcodeNamespace = Namespace,
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            throw new OnvifServerFaultException("Receiver", subcodeName, subcodeNamespace, reason, httpStatusCode);
        }

        /// <summary>Reports a fault caused by the request.</summary>
        [DoesNotReturn]
        public static void ReturnSenderError(
            string reason,
            string subcodeName,
            string subcodeNamespace = Namespace,
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest)
        {
            throw new OnvifServerFaultException("Sender", subcodeName, subcodeNamespace, reason, httpStatusCode);
        }
    }
}
