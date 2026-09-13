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

// Everything a consumer is plausibly holding at once, with no aliases anywhere. This file exists
// to be compiled: a generated type that took a framework name would make the plain names below
// ambiguous and break the build, which is the failure this is here to catch.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using SharpOnvifClient;
using SharpOnvifClient.DeviceMgmt;
using SharpOnvifClient.Media;
using SharpOnvifClient.PTZ;
using SharpOnvifCommon;
using SharpOnvifCommon.Onvif;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The Onvif namespaces have to sit alongside the framework ones without aliases.
    /// <para>
    /// The Onvif schema names several types after things the framework already has - a DateTime,
    /// an IPAddress, a NetworkInterface, an Object. The generator prefixes those with "Onvif" so
    /// importing both is unambiguous; the XML name is untouched, so nothing moves on the wire.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestNamespaceCoexistence
    {
        [TestMethod]
        public void FrameworkNamesStillMeanTheFrameworkTypes()
        {
            DateTime moment = DateTime.UtcNow;
            IPAddress address = IPAddress.Loopback;
            Uri uri = new Uri("http://192.168.1.10/onvif/device_service");
            Encoding encoding = Encoding.UTF8;
            Stream stream = Stream.Null;
            List<string> list = new List<string>();

            Assert.AreNotEqual(default(DateTime), moment);
            Assert.AreEqual("127.0.0.1", address.ToString());
            Assert.AreEqual("/onvif/device_service", uri.AbsolutePath);
            Assert.IsNotNull(encoding);
            Assert.IsNotNull(stream);
            Assert.IsNotNull(list);
            Assert.IsNotNull(NetworkInterface.GetAllNetworkInterfaces());
        }

        [TestMethod]
        public void OnvifNamesAreReachableWithoutQualifying()
        {
            // The renamed contracts, used plainly.
            var when = new OnvifDateTime
            {
                Date = new Date { Year = 2026, Month = 9, Day = 13 },
                Time = new Time { Hour = 12, Minute = 0, Second = 0 },
            };
            var onvifAddress = new OnvifIPAddress { IPv4Address = "192.168.1.10" };
            var scope = new OnvifScope { ScopeItem = "onvif://www.onvif.org/Profile/Streaming" };

            // And the ones that never collided, from the shared namespace and a service namespace.
            var resolution = new VideoResolution { Width = 1920, Height = 1080 };
            var profile = new Profile { token = "p0", Name = "MainStream" };
            var request = new GetDeviceInformationRequest();

            Assert.AreEqual(2026, when.Date.Year);
            Assert.AreEqual("192.168.1.10", onvifAddress.IPv4Address);
            Assert.IsTrue(scope.ScopeItem.Contains("Streaming"));
            Assert.AreEqual(1920, resolution.Width);
            Assert.AreEqual("p0", profile.token);
            Assert.IsNotNull(request);
        }

        [TestMethod]
        public void RenamedContractsKeepTheirSchemaNames()
        {
            // The C# name changed to avoid the clash; what goes on the wire did not.
            AssertSerialisesAs(new OnvifDateTime(), "DateTime");
            AssertSerialisesAs(new OnvifIPAddress(), "IPAddress");
            AssertSerialisesAs(new OnvifScope(), "Scope");
            AssertSerialisesAs(new OnvifNetworkInterface(), "NetworkInterface");
        }

        private static void AssertSerialisesAs(OnvifContract contract, string expected)
        {
            // The type name is what an xsi:type attribute would carry.
            string actual = contract.GetType()
                .GetProperty("OnvifXmlTypeName", System.Reflection.BindingFlags.NonPublic
                                                 | System.Reflection.BindingFlags.Instance)
                .GetValue(contract) as string;

            Assert.AreEqual(expected, actual, $"{contract.GetType().Name} must stay '{expected}' on the wire");
        }
    }
}
