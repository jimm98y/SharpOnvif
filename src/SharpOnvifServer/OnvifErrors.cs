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
