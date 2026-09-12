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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifServer.Dispatch;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How a request finds its operation, for the shapes real clients send.
    /// <para>
    /// Onvif Device Manager does not always put the action in the Content-Type header - for event
    /// subscriptions it carries it in a wsa:Action SOAP header instead - and it addresses a
    /// subscription manager through a reference with the subscription appended to the path. Both
    /// were handled by request-rewriting middleware under CoreWCF, and neither survives that way
    /// on endpoint routing, which chooses an endpoint before application middleware runs.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestOnvifRequestRouting
    {
        private const string DevicePath = "/onvif/device_service";
        private const string SubscriptionPath = "/onvif/Events/PullPointSubscription";

        private static WebApplication _app;
        private static string _baseAddress;

        private sealed class DeviceImpl : SharpOnvifServer.DeviceMgmt.DeviceBase
        {
            public override SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse GetDeviceInformation()
            {
                return new SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse { Manufacturer = "ACME" };
            }
        }

        /// <summary>Reports which subscription the request was addressed to.</summary>
        private sealed class SubscriptionImpl : SharpOnvifServer.Events.EventsBase
        {
            public override SharpOnvifServer.Events.PullMessagesResponse PullMessages(
                string Timeout, int MessageLimit, System.Xml.XmlElement[] Any)
            {
                object id = OnvifOperationContext.Current?.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID];

                return new SharpOnvifServer.Events.PullMessagesResponse
                {
                    // The subscription the address named, echoed back so the test can see it.
                    CurrentTime = new DateTime(2026, 1, 1).AddMinutes(id is int value ? value : 0),
                    TerminationTime = new DateTime(2026, 1, 1),
                };
            }
        }

        [ClassInitialize]
        public static async Task StartServer(TestContext context)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddSingleton<DeviceImpl>();
            builder.Services.AddSingleton<SubscriptionImpl>();

            _app = builder.Build();
            _app.MapOnvifService<DeviceImpl>(DevicePath);
            _app.MapOnvifService<SubscriptionImpl>(SubscriptionPath);

            await _app.StartAsync();

            _baseAddress = _app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses.First().TrimEnd('/');
        }

        [ClassCleanup]
        public static async Task StopServer()
        {
            if (_app != null) await _app.StopAsync();
        }

        private static async Task<(HttpStatusCode Status, string Body)> PostAsync(
            string path, string envelope, string actionInContentType)
        {
            using (var http = new HttpClient())
            {
                var content = new StringContent(envelope);
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                    actionInContentType == null
                        ? "application/soap+xml; charset=utf-8"
                        : $"application/soap+xml; charset=utf-8; action=\"{actionInContentType}\"");

                var response = await http.PostAsync(_baseAddress + path, content);
                return (response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }

        private const string DeviceInformationBody =
            "<?xml version=\"1.0\"?>" +
            "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body></s:Envelope>";

        private const string DeviceInformationAction =
            "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation";

        [TestMethod]
        public async Task FindsTheOperationFromTheContentTypeAction()
        {
            var (status, body) = await PostAsync(DevicePath, DeviceInformationBody, DeviceInformationAction);

            Assert.AreEqual(HttpStatusCode.OK, status);
            StringAssert.Contains(body, "ACME");
        }

        [TestMethod]
        public async Task FindsTheOperationFromAWsAddressingHeader()
        {
            // What Onvif Device Manager sends: no action on the Content-Type, one in the header.
            string envelope =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" " +
                "xmlns:a=\"http://www.w3.org/2005/08/addressing\">" +
                $"<s:Header><a:Action>{DeviceInformationAction}</a:Action></s:Header>" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body></s:Envelope>";

            var (status, body) = await PostAsync(DevicePath, envelope, actionInContentType: null);

            Assert.AreEqual(HttpStatusCode.OK, status);
            StringAssert.Contains(body, "ACME");
        }

        [TestMethod]
        public async Task FindsTheOperationFromTheBodyElementAlone()
        {
            // Neither form of action; the body element is the last thing left to go on.
            var (status, body) = await PostAsync(DevicePath, DeviceInformationBody, actionInContentType: null);

            Assert.AreEqual(HttpStatusCode.OK, status);
            StringAssert.Contains(body, "ACME");
        }

        [TestMethod]
        public async Task ReachesASubscriptionManagerThroughItsReferenceAddress()
        {
            // Onvif hands a client a subscription reference with the subscription appended, and
            // every later call goes there. A client whose PullMessages does not arrive abandons
            // the subscription and makes a new one on every poll.
            string envelope =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Body><PullMessages xmlns=\"http://www.onvif.org/ver10/events/wsdl\">" +
                "<Timeout>PT1S</Timeout><MessageLimit>10</MessageLimit>" +
                "</PullMessages></s:Body></s:Envelope>";

            string action = "http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesRequest";

            foreach (string address in new[] { SubscriptionPath + "/7/", SubscriptionPath + "/7" })
            {
                var (status, body) = await PostAsync(address, envelope, action);

                Assert.AreEqual(HttpStatusCode.OK, status, $"{address} did not reach the subscription manager");

                // The implementation encodes the subscription it saw into the minutes field.
                StringAssert.Contains(body, "2026-01-01T00:07:00", $"{address} lost the subscription");
            }
        }

        [TestMethod]
        public async Task StillServesTheAddressWithoutASubscription()
        {
            string envelope =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Body><PullMessages xmlns=\"http://www.onvif.org/ver10/events/wsdl\">" +
                "<Timeout>PT1S</Timeout><MessageLimit>10</MessageLimit>" +
                "</PullMessages></s:Body></s:Envelope>";

            var (status, _) = await PostAsync(
                SubscriptionPath, envelope,
                "http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesRequest");

            Assert.AreEqual(HttpStatusCode.OK, status);
        }
    }
}
