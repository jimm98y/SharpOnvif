using System;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SharpOnvifCommon.Xml;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>What an operation produced, and the element its body should be written as.</summary>
    public struct OnvifDispatchResult
    {
        /// <summary>The response contract, or null for an operation with no reply body.</summary>
        public OnvifContract Response;

        /// <summary>Namespace of the response body element.</summary>
        public string Namespace;

        /// <summary>Local name of the response body element.</summary>
        public string ElementName;

        public OnvifDispatchResult(OnvifContract response, string ns, string elementName)
        {
            Response = response;
            Namespace = ns;
            ElementName = elementName;
        }
    }

    /// <summary>
    /// Routes a SOAP action to a method on a service implementation. One is generated per portType.
    /// <para>
    /// Dispatchers are stateless and are registered once per endpoint, which is what lets several
    /// services share a single URL. The CoreWCF bindings this replaces could not do that, and the
    /// sample had to spread Onvif across /onvif/device_service, /onvif/media_service and so on.
    /// </para>
    /// </summary>
    public abstract class OnvifServiceDispatcher
    {
        /// <summary>The generated base class this dispatcher invokes.</summary>
        public abstract Type ServiceType { get; }

        /// <summary>True when this dispatcher implements the given SOAP action.</summary>
        public abstract bool CanHandle(string action);

        /// <summary>
        /// Maps a request body element back to its SOAP action, for clients that send no action
        /// in the Content-Type header.
        /// </summary>
        public abstract bool TryResolveAction(string ns, string elementName, out string action);

        /// <summary>
        /// Reads the request body and invokes the operation. The reader is positioned on the body
        /// element.
        /// </summary>
        public abstract Task<OnvifDispatchResult> InvokeAsync(
            object service, string action, XmlReader body, CancellationToken cancellationToken);

        /// <summary>Resolves an xsi:type in this dispatcher's assembly.</summary>
        public abstract OnvifContract ResolveXmlType(string ns, string name);

        /// <summary>
        /// Reads a request body with this service's own view of the shared schema types.
        /// </summary>
        /// <remarks>
        /// Here rather than in the generated dispatcher because what reads XML is this library's
        /// choice: the generated code knows only the interface it drives.
        /// </remarks>
        protected IXmlReader CreateReader(XmlReader body)
        {
            return new OnvifXmlReader(body, ResolveXmlType);
        }
    }
}
