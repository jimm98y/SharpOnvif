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
                var id = OnvifOperationContext.Current?.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID] as string;

                return new SharpOnvifServer.Events.PullMessagesResponse
                {
                    // The subscription the address named, echoed back so the test can see it.
                    // An ID is an opaque token, so it is returned as it arrived.
                    CurrentTime = new DateTime(2026, 1, 1),
                    TerminationTime = new DateTime(2026, 1, 1),
                    NotificationMessage = new[] { Echo(id) },
                };
            }
        }

        /// <summary>
        /// Carries the subscription the request was addressed to back to the test, in the topic
        /// of a notification - which is where a real pull point would put one.
        /// </summary>
        private static SharpOnvifCommon.Onvif.NotificationMessageHolderType Echo(string subscriptionId)
        {
            var dom = new System.Xml.XmlDocument();

            return new SharpOnvifCommon.Onvif.NotificationMessageHolderType
            {
                Topic = new SharpOnvifCommon.Onvif.TopicExpressionType
                {
                    Dialect = OnvifEvents.TopicDialectConcreteSet,
                    Any = new System.Xml.XmlNode[] { dom.CreateTextNode(subscriptionId ?? "(none)") },
                },
            };
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
        public async Task WillNotLetAnActionBreakOutOfTheTextItIsWrittenInto()
        {
            // The action is whatever the caller chose to send. A newline in it, written into a log
            // entry or a fault reason unchanged, starts what reads as a new line of its own - so a
            // caller could compose log entries, or fault text, that never happened.
            const string Forged =
                "Probe\r\nfail: SharpOnvifServer[0]\r\n      Administrator logged in from 10.0.0.9";

            string envelope =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" " +
                "xmlns:a=\"http://www.w3.org/2005/08/addressing\">" +
                $"<s:Header><a:Action>{Forged}</a:Action></s:Header>" +
                "<s:Body><Nonexistent xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body></s:Envelope>";

            var (status, body) = await PostAsync(DevicePath, envelope, actionInContentType: null);

            // The reply is a fault that quotes the action back, which is the same string the
            // endpoint logs.
            StringAssert.Contains(body, "ActionNotSupported");

            int reasonStart = body.IndexOf("Administrator logged in", StringComparison.Ordinal);
            Assert.IsTrue(reasonStart > 0, "the fault has to quote the action, or this proves nothing");

            string quoted = body.Substring(0, reasonStart);
            int lastNewline = quoted.LastIndexOf('\n');
            int lastQuote = quoted.LastIndexOf("action '", StringComparison.Ordinal);
            Assert.IsTrue(lastQuote > lastNewline,
                "the action broke onto a line of its own instead of staying inside the text quoting it");

            // The XML parser normalises the CRLF in element content to a single LF before the
            // endpoint ever sees it, so one escape is what is left to find.
            StringAssert.Contains(body, "Probe\\nfail:",
                "the newline has to survive as an escape, so the value is still readable");
        }

        [TestMethod]
        public async Task RefusesARequestLargerThanItWillRead()
        {
            // The envelope is held as a string and read more than once, so its size is paid for
            // several times over. How much that is cannot be the caller's choice.
            string padding = new string('x', OnvifEndpoint.MaxRequestBytes + 1024);
            string envelope =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\">" +
                $"<Padding>{padding}</Padding>" +
                "</GetDeviceInformation></s:Body></s:Envelope>";

            var (status, body) = await PostAsync(DevicePath, envelope, DeviceInformationAction);

            Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, status);
            StringAssert.Contains(body, "larger than this endpoint accepts");
        }

        [TestMethod]
        public async Task StillReadsARequestOfAnOrdinarySize()
        {
            // The limit has to be well clear of anything real: a configuration being written is
            // the largest Onvif request there is, and it is nothing like a megabyte.
            string padding = new string('x', 64 * 1024);
            string envelope =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\">" +
                $"<Padding>{padding}</Padding>" +
                "</GetDeviceInformation></s:Body></s:Envelope>";

            var (status, body) = await PostAsync(DevicePath, envelope, DeviceInformationAction);

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

            // An ID is an opaque token now, not a number, so that one client cannot address
            // another client's subscription by counting.
            const string SubscriptionId = "Zm9vYmFyYmF6cXV4-_1";

            foreach (string address in new[] { SubscriptionPath + "/" + SubscriptionId + "/",
                                               SubscriptionPath + "/" + SubscriptionId })
            {
                var (status, body) = await PostAsync(address, envelope, action);

                Assert.AreEqual(HttpStatusCode.OK, status, $"{address} did not reach the subscription manager");

                StringAssert.Contains(body, SubscriptionId, $"{address} lost the subscription");
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
