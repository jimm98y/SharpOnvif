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
using SharpOnvifClient;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The address a camera is handed for Basic subscription notifications.
    /// <para>
    /// Onvif gives a camera no way to authenticate itself to this endpoint: a notification is a
    /// plain POST, and anything that can reach the port can deliver one. What keeps an application
    /// from acting on somebody else's notification is that the address it was told to post to
    /// cannot be guessed.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestEventListenerSecurity
    {
        private static SimpleOnvifEventListener Listener() =>
            new SimpleOnvifEventListener("127.0.0.1", 19999);

        [TestMethod]
        public void HandsOutAnAddressThatCannotBeGuessed()
        {
            using (var listener = Listener())
            {
                string uri = listener.GetOnvifEventListenerUri(3);

                StringAssert.StartsWith(uri, "http://127.0.0.1:19999/");
                StringAssert.EndsWith(uri, "/3/", "the camera it belongs to is still the last segment");

                string token = listener.PathToken;
                Assert.IsTrue(token.Length >= 20, $"'{token}' is too short to be unguessable");
                StringAssert.Contains(uri, token);

                foreach (char c in token)
                {
                    Assert.IsTrue(char.IsLetterOrDigit(c) || c == '-' || c == '_',
                        $"'{c}' does not belong in a URL");
                }
            }
        }

        [TestMethod]
        public void GivesEveryListenerAnAddressOfItsOwn()
        {
            using (var first = Listener())
            using (var second = Listener())
            {
                Assert.AreNotEqual(first.PathToken, second.PathToken);
            }
        }

        [TestMethod]
        public void AcceptsOnlyTheAddressItHandedOut()
        {
            using (var listener = Listener())
            {
                string token = listener.PathToken;

                Assert.IsTrue(listener.TryReadAddress($"/{token}/7/", out int cameraID));
                Assert.AreEqual(7, cameraID);

                Assert.IsTrue(listener.TryReadAddress($"/{token}/7", out cameraID), "a trailing slash is optional");
                Assert.AreEqual(7, cameraID);

                Assert.IsTrue(listener.TryReadAddress($"/{token}/7/?x=1", out _), "a query string is not part of the path");

                Assert.IsFalse(listener.TryReadAddress("/7/", out _),
                    "the bare address a caller would guess must not be accepted");
                Assert.IsFalse(listener.TryReadAddress($"/{token}x/7/", out _));
                Assert.IsFalse(listener.TryReadAddress($"/{token.ToUpperInvariant()}/7/", out _),
                    "the token is compared exactly");
                Assert.IsFalse(listener.TryReadAddress($"/{token}/", out _), "no camera named");
                Assert.IsFalse(listener.TryReadAddress($"/{token}/notanumber/", out _));
                Assert.IsFalse(listener.TryReadAddress("/", out _));
                Assert.IsFalse(listener.TryReadAddress(null, out _));
            }
        }

        [TestMethod]
        public void CanBeToldToHandOutABareAddressInstead()
        {
            // What the listener did before, for a device that cannot be given a long path.
            using (var listener = Listener())
            {
                listener.PathToken = null;

                Assert.AreEqual("http://127.0.0.1:19999/0/", listener.GetOnvifEventListenerUri());
                Assert.IsTrue(listener.TryReadAddress("/0/", out int cameraID));
                Assert.AreEqual(0, cameraID);
            }
        }

        [TestMethod]
        public void AcceptsDeliveryFromAnywhereUntilToldOtherwise()
        {
            using (var listener = Listener())
            {
                Assert.AreEqual(0, listener.AllowedSources.Count,
                    "restricting delivery is a choice, because the camera's address is not always known");
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public async System.Threading.Tasks.Task RefusesANotificationSentToAnAddressItNeverHandedOut()
        {
            // The whole point, end to end: something that found the port but not the address gets
            // nowhere, and the application never hears about it.
            using (var listener = new SimpleOnvifEventListener("127.0.0.1", 19998))
            {
                var delivered = new List<string>();
                listener.Start((camera, notification) =>
                {
                    lock (delivered) delivered.Add($"{camera}:{notification}");
                });

                using (var http = new System.Net.Http.HttpClient())
                {
                    var guessed = await http.PostAsync(
                        "http://127.0.0.1:19998/0/",
                        new System.Net.Http.StringContent("<forged/>"));

                    Assert.AreEqual(System.Net.HttpStatusCode.NotFound, guessed.StatusCode,
                        "the address a caller would guess has to lead nowhere");

                    var real = await http.PostAsync(
                        listener.GetOnvifEventListenerUri(4),
                        new System.Net.Http.StringContent("<genuine/>"));

                    Assert.AreEqual(System.Net.HttpStatusCode.OK, real.StatusCode);

                    var wrongMethod = await http.GetAsync(listener.GetOnvifEventListenerUri(4));
                    Assert.AreEqual(System.Net.HttpStatusCode.MethodNotAllowed, wrongMethod.StatusCode);
                }

                lock (delivered)
                {
                    CollectionAssert.AreEqual(new[] { "4:<genuine/>" }, delivered,
                        "only the notification sent to the address handed out reaches the application");
                }

                Assert.AreEqual(2, listener.RefusedCount);
            }
        }

        [TestMethod]
        public void BoundsWhatANotificationMayCost()
        {
            using (var listener = Listener())
            {
                Assert.IsTrue(listener.MaxNotificationBytes > 0);
                Assert.IsTrue(listener.MaxNotificationBytes <= 16 * 1024 * 1024,
                    "a notification is a few kilobytes; the cap is what something else can make us allocate");
            }
        }
    }
}
