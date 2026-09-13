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

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SharpOnvifCommon.Security;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// What an unauthenticated caller can reach.
    /// <para>
    /// Onvif puts a handful of operations in its PRE_AUTH class, which a device answers without
    /// credentials, and the caller says which operation it wants. That makes the request itself
    /// decide whether the device asks for a password - so what the request says has to be read
    /// once, and mean the same thing to the part that authenticates and the part that dispatches.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestAuthenticationBypass
    {
        private const string GetDeviceInformation =
            "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation";

        private const string GetSystemDateAndTime =
            "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime";

        private static StringContent Body(string operation)
        {
            var content = new StringContent(
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                $"<s:Body><{operation} xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body>" +
                "</s:Envelope>");
            content.Headers.Clear();
            return content;
        }

        /// <summary>Sends one request with the Content-Type spelled exactly as given.</summary>
        private static async Task<HttpResponseMessage> PostAsync(
            AuthenticatedDevice device, string contentType, string operation, string authorization = null)
        {
            var http = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, device.Endpoint) { Content = Body(operation) };
            request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            if (authorization != null)
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

            return await http.SendAsync(request);
        }

        [TestMethod]
        public async Task RefusesAPrivilegedOperationSmuggledPastTheOneThatNeedsNoPassword()
        {
            // The device authenticates on one reading of the Content-Type and dispatches on
            // another: it looked for a quoted PRE_AUTH action anywhere in the header, while the
            // endpoint takes whatever follows the first "action=". Naming an unquoted operation
            // first and a quoted PRE_AUTH one second satisfied the check and ran the other.
            await using var device = await AuthenticatedDevice.StartAsync();

            var response = await PostAsync(
                device,
                $"application/soap+xml; charset=utf-8; action={GetDeviceInformation}; action=\"{GetSystemDateAndTime}\"",
                "GetDeviceInformation");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
                "an unauthenticated caller reached an operation that needs a password");
        }

        [TestMethod]
        public async Task StillAnswersAnOperationThatNeedsNoPassword()
        {
            // The other half: the class exists to be used, so an honest PRE_AUTH request must go
            // through without credentials.
            await using var device = await AuthenticatedDevice.StartAsync();

            var response = await PostAsync(
                device,
                $"application/soap+xml; charset=utf-8; action=\"{GetSystemDateAndTime}\"",
                "GetSystemDateAndTime");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task StillAsksForAPasswordForEverythingElse()
        {
            await using var device = await AuthenticatedDevice.StartAsync();

            var response = await PostAsync(
                device,
                $"application/soap+xml; charset=utf-8; action=\"{GetDeviceInformation}\"",
                "GetDeviceInformation");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task ProvesItKnowsThePasswordOnlyToSomebodyWhoProvedTheyDo()
        {
            // Authentication-Info carries rspauth, a digest over the device's own reply computed
            // with the password. It was written for any request that carried an Authorization
            // header at all, whether or not that header checked out - so an unauthenticated caller
            // could ask a PRE_AUTH operation, supply a nonce, cnonce and realm of their choosing,
            // and be handed a digest of the real password over values they picked. That is an
            // offline password oracle.
            await using var device = await AuthenticatedDevice.StartAsync(
                options => options.Onvif.Authentication = DigestAuthentication.HttpDigest);

            var response = await PostAsync(
                device,
                $"application/soap+xml; charset=utf-8; action=\"{GetSystemDateAndTime}\"",
                "GetSystemDateAndTime",
                authorization:
                    "Digest username=\"admin\", realm=\"chosen by the caller\", nonce=\"chosen\", " +
                    "uri=\"/onvif/device_service\", response=\"" + new string('0', 32) + "\", " +
                    "qop=auth, nc=00000001, cnonce=\"chosen\"");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "the operation itself needs no password");

            Assert.IsFalse(response.Headers.Contains("Authentication-Info"),
                "the device proved it knows the password to a caller who never proved anything");
        }
    }
}
