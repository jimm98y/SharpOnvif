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
    /// <summary>
    /// The arithmetic of a WS-UsernameToken password digest: <c>Base64(SHA1(nonce + created +
    /// password))</c>, with the nonce hashed as the bytes it decodes to rather than as its text.
    /// <para>
    /// Pinned against expected values rather than against the library's own output. A client and a
    /// server built from the same code agree with each other whatever this computes - swapping
    /// SHA-1 for SHA-256 leaves them authenticating happily - so agreeing with the specification
    /// is what says a real camera will accept it. That is this test; that the two sides of this
    /// library then agree is TestWsUsernameTokenEndToEnd.
    /// </para>
    /// </summary>
    [TestClass]
    public class TestWsUsernamePassword
    {
        [DataRow("LKqI6G/AikKCQrN0zqZFlg==", "2010-09-16T07:50:45Z", "userpassword", "tuOSpGlFlIXsozq4HFNeeGeFLEI=",
            DisplayName = "the Onvif specification's own example")]
        [DataRow("AAAAAAAAAAAAAAAAAAAAAA==", "2026-01-01T00:00:00Z", "password", "mqmvaHdW1+plgGJ7VWoS0c4UD+s=",
            DisplayName = "a nonce of nothing but zeroes")]
        [DataRow("d2hhdGV2ZXIxMjM0NTY3OA==", "2026-06-15T12:34:56.789Z", "p@ssw0rd!", "LObiQVNxPa5DUDCAlV9y/T4GXJg=",
            DisplayName = "fractional seconds and punctuation in the password")]
        [DataRow("LKqI6G/AikKCQrN0zqZFlg==", "2010-09-16T07:50:45Z", "", "7Rs/BJ5CcRVV71gaoAqhN9AvzbI=",
            DisplayName = "an empty password")]
        [TestMethod]
        public void TestUserNamePassword(string nonce, string date, string password, string resultDigest)
        {
            string digest = WsDigestAuthentication.CreateSoapDigest(nonce, date, password);
            Assert.AreEqual(resultDigest, digest);
        }
    }
}
