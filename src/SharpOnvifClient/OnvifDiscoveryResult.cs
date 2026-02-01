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

namespace SharpOnvifClient
{
    /// <summary>
    /// Discovered device.
    /// </summary>
    public class OnvifDiscoveryResult
    {
        /// <summary>
        /// Raw SOAP message for advanced processing.
        /// </summary>
        public string Raw { get; set; }

        /// <summary>
        /// Onvif addresses.
        /// </summary>
        public string[] Addresses { get; set; }

        /// <summary>
        /// Onvif scopes.
        /// </summary>
        public string[] Scopes { get; set; }

        /// <summary>
        /// Location - city.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Location - country.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Hardware.
        /// </summary>
        public string Hardware { get; set; }

        /// <summary>
        /// MAC address.
        /// </summary>
        public string MAC { get; set; }

        /// <summary>
        /// Device manufacturer.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Device name.
        /// </summary>
        public string Name { get; set; }
    }
}
