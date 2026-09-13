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

using SharpOnvifCommon.Security;

namespace SharpOnvif.Tests
{
    [TestClass]
    public sealed class TestRFC2617
    {
        // RFC 2617 uses a different password than RFC 7616, the difference is only in the capital 'O': "Circle Of Life" vs "Circle of Life"
        [DataRow("MD5", "Mufasa", "Circle Of Life", "testrealm@host.com", "/dir/index.html", "dcd98b7102dd2f0e8b11d0f600bfb0c093", "GET", "5ccc069c403ebaf9f0171e9517f40e41", 1, "0a4f113b", "auth", "6629fae49393a05397450978507c4ef1")]
        [TestMethod]
        public void TestDigest(string algorithm, string userName, string password, string realm, string uri, string nonce, string method, string opaque, int nc, string cnonce, string qop, string expectedResponse)
        {
            string digest = HttpDigestAuthentication.CreateWebDigestRFC2617(algorithm, userName, realm, password, false, nonce, method, uri, nc, cnonce, qop);
            Assert.AreEqual(expectedResponse, digest);
        }
    }
}
