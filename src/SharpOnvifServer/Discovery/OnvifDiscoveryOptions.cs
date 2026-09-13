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

namespace SharpOnvifServer.Discovery
{
    public class OnvifDiscoveryOptions
    {
        /// <summary>
        /// The device's own address in WS-Discovery, as a urn:uuid. Every announcement the device
        /// makes names it, and a client pairs a Bye with the Hello and the ProbeMatch that carried
        /// the same one.
        /// </summary>
        /// <remarks>
        /// Left unset, an address is generated that lasts as long as the process. A device that
        /// survives a restart should set this from something that survives with it - its serial
        /// number or MAC, hashed - or a client will believe the old device vanished and a new one
        /// appeared every time it is restarted.
        /// </remarks>
        public string EndpointReference { get; set; }

        /// <summary>The address used when <see cref="EndpointReference"/> is not set.</summary>
        internal string RuntimeEndpointReference { get; } = "urn:uuid:" + Guid.NewGuid().ToString().ToLowerInvariant();

        /// <summary>
        /// Incremented by the device whenever what it advertises changes, so that a client which
        /// has seen the device before knows whether to read it again.
        /// </summary>
        public int MetadataVersion { get; set; } = 10;

        /// <summary>
        /// Whether the device announces itself with Hello when it starts and Bye when it stops.
        /// Without it a client only learns of the device by probing for it.
        /// </summary>
        public bool AnnounceOnStartAndStop { get; set; } = true;

        public List<string> NetworkInterfaces { get; set; }
        public List<string> ServiceAddresses { get; set; }
        public List<string> Scopes { get; set; }
        public List<OnvifType> Types { get; set; }
        public string MAC { get; set; }
        public string Manufacturer { get; set; }
        public string Hardware { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}
