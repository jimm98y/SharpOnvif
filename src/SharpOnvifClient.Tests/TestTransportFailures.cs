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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifClient.DeviceMgmt;
using SharpOnvifCommon.Soap;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// What a caller sees when the device does not answer.
    /// <para>
    /// A device reboots, loses power, or is simply stopped, and a pull point subscription spends
    /// nearly all of its time waiting on a request that any of those will cut short. The HTTP
    /// stack has several ways of saying so and none of them mention Onvif - a dropped connection
    /// is an HttpIOException inside an HttpRequestException, a timeout is a TaskCanceledException -
    /// so an application either catches all of them or, as the sample did, crashes.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestTransportFailures
    {
        private const string Endpoint = "http://192.168.1.10/onvif/device_service";

        /// <summary>A transport that fails the way a device going away does.</summary>
        private sealed class BrokenTransport : HttpMessageHandler
        {
            private readonly Func<Exception> _fail;

            public BrokenTransport(Func<Exception> fail) { _fail = fail; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw _fail();
            }
        }

        /// <summary>A transport that never answers, like a device that has stopped listening.</summary>
        private sealed class SilentTransport : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException("unreachable");
            }
        }

        private static DeviceClient Client(HttpMessageHandler transport, TimeSpan? timeout = null) =>
            new DeviceClient(Endpoint, new OnvifClientSettings
            {
                Transport = transport,
                Timeout = timeout ?? TimeSpan.FromSeconds(30),
            });

        [TestMethod]
        public async Task SaysSoWhenTheDeviceDropsTheConnection()
        {
            // What stopping a device does to a client waiting on a pull.
            using (var client = Client(new BrokenTransport(
                () => new HttpRequestException("An error occurred while sending the request.",
                          new IOException("The response ended prematurely.")))))
            {
                var failure = await Assert.ThrowsExactlyAsync<SoapTransportException>(
                    () => client.GetDeviceInformationAsync());

                StringAssert.Contains(failure.Message, Endpoint, "the address is worth knowing");
                Assert.IsFalse(failure.TimedOut, "the device answered nothing at all, it did not run late");
                Assert.IsInstanceOfType<HttpRequestException>(failure.InnerException,
                    "what actually happened has to survive");
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task SaysSoWhenTheDeviceDoesNotAnswerInTime()
        {
            using (var client = Client(new SilentTransport(), TimeSpan.FromMilliseconds(300)))
            {
                var failure = await Assert.ThrowsExactlyAsync<SoapTransportException>(
                    () => client.GetDeviceInformationAsync());

                Assert.IsTrue(failure.TimedOut, "a device that runs out of time is worth telling apart");
                StringAssert.Contains(failure.Message, "timed out");
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public async Task LetsACancellationTheCallerAskedForThrough()
        {
            // The caller stopping its own request is not the device failing, and must not be
            // reported as one - an application shutting down would otherwise look like a fault.
            using (var client = Client(new SilentTransport()))
            using (var cancellation = new CancellationTokenSource())
            {
                Task pending = client.GetDeviceInformationAsync(
                    new GetDeviceInformationRequest(), cancellation.Token);

                cancellation.Cancel();

                await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => pending);
            }
        }
    }
}
