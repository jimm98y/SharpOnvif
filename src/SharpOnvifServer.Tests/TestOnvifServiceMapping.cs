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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifServer.Dispatch;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// What MapOnvifService records about what it has mapped, and where it records it.
    /// <para>
    /// The record used to be one static dictionary keyed by route builder, shared by every
    /// application in the process with nothing between them. Two applications being built at once
    /// corrupted it, and nothing ever removed an application that had finished being built, so it
    /// held every endpoint it had ever seen for the life of the process.
    /// </para>
    /// <para>
    /// The race itself is not tested here: reproducing it took four dozen applications mapping at
    /// one moment and still only failed one run in three, which is half a minute spent to learn
    /// almost nothing. What is tested is the sharing that caused it. An application that lets go
    /// of everything the mapping left behind is an application whose record was never anywhere
    /// another could reach, and two builds cannot corrupt a dictionary they do not share.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestOnvifServiceMapping
    {
        private const string DevicePath = "/onvif/device_service";

        private sealed class DeviceImpl : SharpOnvifServer.DeviceMgmt.DeviceBase
        {
            public override SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse GetDeviceInformation()
            {
                return new SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse { Manufacturer = "ACME" };
            }
        }

        private sealed class MediaImpl : SharpOnvifServer.Media.MediaBase
        {
        }

        private static WebApplication Build()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton<DeviceImpl>();
            builder.Services.AddSingleton<MediaImpl>();
            return builder.Build();
        }

        /// <summary>The routes MapOnvifService added, which is what a request is matched against.</summary>
        private static List<string> RoutesOf(WebApplication app)
        {
            // WebApplication carries its data sources as an explicit implementation.
            return ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .OfType<RouteEndpoint>()
                .Select(endpoint => endpoint.RoutePattern.RawText)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToList();
        }

        [TestMethod]
        public void LetsAnApplicationGoOnceItIsFinishedWith()
        {
            // The static that held this record had no way to remove an entry, so every
            // application ever built stayed reachable through it, with its endpoints, for the
            // life of the process. Being shared is also what let two builds at once corrupt it,
            // so this is the same fault seen from the side that can be asserted on: the record
            // now lives on the builder, and goes when the builder goes.
            WeakReference application = MapThenAbandon();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(application.IsAlive,
                "an application that has been disposed is still held by something the mapping left behind");
        }

        /// <summary>
        /// Maps a service, disposes the application and keeps no strong reference to it. Its own
        /// method so that the local does not stay alive on the frame of the test that called it.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference MapThenAbandon()
        {
            WebApplication app = Build();
            app.MapOnvifService<DeviceImpl>(DevicePath);
            ((IDisposable)app).Dispose();

            return new WeakReference(app);
        }

        [TestMethod]
        public async Task GivesEachApplicationItsOwnRecordOfWhatIsMapped()
        {
            // Two applications mapping the same path are two devices, not one. Sharing the record
            // between them would have the second join the first's endpoint and map no route of
            // its own, which is a device that answers nothing.
            await using WebApplication first = Build();
            await using WebApplication second = Build();

            first.MapOnvifService<DeviceImpl>(DevicePath);
            second.MapOnvifService<DeviceImpl>(DevicePath);

            CollectionAssert.AreEqual(RoutesOf(first), RoutesOf(second));
            Assert.AreEqual(2, RoutesOf(second).Count, "the second application mapped no route of its own");
        }

        [TestMethod]
        public async Task StillPutsTwoServicesOnOnePathWhenItIsOneApplication()
        {
            // The whole reason the record exists: a device that serves everything from one URL,
            // where the second service joins the endpoint the first created rather than mapping a
            // route that would collide with it.
            await using WebApplication app = Build();

            app.MapOnvifService<DeviceImpl>(DevicePath);
            app.MapOnvifService<MediaImpl>(DevicePath);

            CollectionAssert.AreEqual(
                new[] { DevicePath, DevicePath + "/{" + OnvifEndpoint.SubscriptionRouteValue + "}" },
                RoutesOf(app),
                "the second service mapped a route of its own instead of joining the first");
        }

        [TestMethod]
        public async Task KeepsTheRecordOffTheRoutingTable()
        {
            // The record rides along in the builder's data sources, which is also where the routes
            // live. It must not put an endpoint of its own in there.
            await using WebApplication app = Build();

            app.MapOnvifService<DeviceImpl>(DevicePath);

            IEnumerable<Endpoint> notRoutes = ((IEndpointRouteBuilder)app).DataSources
                .SelectMany(source => source.Endpoints)
                .Where(endpoint => !(endpoint is RouteEndpoint));

            Assert.IsFalse(notRoutes.Any(), "the record contributed an endpoint to the routing table");
        }
    }
}
