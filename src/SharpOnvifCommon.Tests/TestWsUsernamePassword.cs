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
    public class TestWsUsernamePassword
    {
        [DataRow("LKqI6G/AikKCQrN0zqZFlg==", "2010-09-16T07:50:45Z", "userpassword", "tuOSpGlFlIXsozq4HFNeeGeFLEI=")]
        [TestMethod]
        public void TestUserNamePassword(string nonce, string date, string password, string resultDigest)
        {
            string digest = WsDigestAuthentication.CreateSoapDigest(nonce, date, password);
            Assert.AreEqual(resultDigest, digest);
        }
    }
}
