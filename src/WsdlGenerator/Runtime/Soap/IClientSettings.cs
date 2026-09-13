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
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using __RUNTIME__.Xml;

namespace __RUNTIME__.Soap
{
    /// <summary>
    /// What a generated client was built with: who it is, where it reports, how it reaches the
    /// service, and what it declares on the messages it sends.
    /// </summary>
    /// <remarks>
    /// Only the contract is generated. Which defaults are sensible is not - a sixteen megabyte
    /// response cap and a suppressed Expect: 100-continue are what talking to cameras taught, not
    /// what WSDL says - so the defaults belong to whatever implements this.
    /// </remarks>
    public interface IClientSettings
    {
        /// <summary>
        /// What writes a message and reads the reply. A client cannot be built without one - there
        /// is nothing it could fall back to, having no idea what an envelope looks like.
        /// </summary>
        IMessageCodec Codec { get; }

        /// <summary>Credentials, or null for an unauthenticated client.</summary>
        NetworkCredential Credentials { get; }

        /// <summary>Where the client reports what it could not do, or null for nowhere.</summary>
        ILog Logger { get; }

        /// <summary>How the client proves who it is, or null to send no credentials at all.</summary>
        IClientAuthentication Authentication { get; }

        /// <summary>
        /// How far the service's clock is ahead of this machine's.
        /// </summary>
        /// <remarks>
        /// The client itself does not care. A scheme that stamps its credentials with a time does:
        /// it decides whether to send them in the service's time rather than in this machine's,
        /// which is what gets a client past a device whose clock nobody can reach to correct.
        /// </remarks>
        TimeSpan UtcNowOffset { get; }

        /// <summary>How long a single call may take.</summary>
        TimeSpan Timeout { get; }

        /// <summary>Whether to suppress the Expect: 100-continue request header.</summary>
        bool DisableExpect100Continue { get; }

        /// <summary>Largest response the client will buffer, in bytes. Zero means no limit.</summary>
        long MaxResponseContentBytes { get; }

        /// <summary>The transport handler to send through, or null to let the client build one.</summary>
        HttpMessageHandler Transport { get; }

        /// <summary>
        /// An <see cref="System.Net.Http.HttpClient"/> to send through instead of one built from
        /// the rest of these, or null. It is not disposed with the service client.
        /// </summary>
        HttpClient HttpClient { get; }

        /// <summary>
        /// Prefixes declared on the envelope element of every message, whether or not the body
        /// uses them, or null for none.
        /// </summary>
        /// <remarks>
        /// Which ones is the service's convention rather than SOAP's: devices and tools are known
        /// to expect the prefixes they have always been sent, and a prefix used inside element
        /// content has to be in scope wherever that content ends up.
        /// </remarks>
        IEnumerable<XmlNamespaceDeclaration> EnvelopePrologue { get; }
    }
}
