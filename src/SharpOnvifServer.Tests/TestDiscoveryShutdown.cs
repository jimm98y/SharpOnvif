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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifServer;
using SharpOnvifServer.Discovery;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Stopping the discovery service.
    /// <para>
    /// A host that will not shut down has to be killed, and a killed host leaves its sockets open
    /// and its multicast group joined until the operating system cleans up after it. The listener
    /// used to sit in a synchronous Receive, which closing the socket does not interrupt on Unix -
    /// the thread stays in recvfrom - so shutdown waited on a task that was never going to finish.
    /// A developer pressing Ctrl-C got a process that ignored it, and every run left another one
    /// behind holding port 3702.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestDiscoveryShutdown
    {
        /// <summary>Generous next to the service's own five second bound, tight next to forever.</summary>
        private static readonly TimeSpan Bound = TimeSpan.FromSeconds(20);

        private static WebApplication CreateDevice()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddOnvifDiscovery(new OnvifDiscoveryOptions
            {
                Scopes = new List<string> { "onvif://www.onvif.org/Profile/Streaming" },
                Types = new List<OnvifType>
                {
                    new OnvifType("http://www.onvif.org/ver10/network/wsdl", "NetworkVideoTransmitter"),
                },
            });

            return builder.Build();
        }

        [TestMethod]
        [Timeout(120000)]
        public async Task StopsWhenItIsAskedTo()
        {
            WebApplication app = CreateDevice();

            await app.StartAsync();

            // The listeners are started from ApplicationStarted, which runs after StartAsync
            // returns, so give them a moment to be there to stop.
            await Task.Delay(TimeSpan.FromSeconds(2));

            var stopwatch = Stopwatch.StartNew();
            Task stopping = app.StopAsync();

            Task finished = await Task.WhenAny(stopping, Task.Delay(Bound));
            stopwatch.Stop();

            Assert.AreSame(stopping, finished,
                $"the host was still shutting down after {Bound.TotalSeconds} seconds, which is how a " +
                "process ends up having to be killed with its sockets still open");

            await stopping;
            await app.DisposeAsync();
        }

        [TestMethod]
        [Timeout(120000)]
        public async Task StopsAgainAfterItHasAlreadyRunOnce()
        {
            // Two in a row: the second only starts if the first really let go of port 3702.
            for (int run = 0; run < 2; run++)
            {
                WebApplication app = CreateDevice();
                await app.StartAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));

                Task stopping = app.StopAsync();
                Assert.AreSame(stopping, await Task.WhenAny(stopping, Task.Delay(Bound)),
                    $"run {run} would not stop");

                await stopping;
                await app.DisposeAsync();
            }
        }
    }
}
