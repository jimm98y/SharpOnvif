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
            string digest = DigestHelpers.CreateWebDigestRFC2617(algorithm, userName, realm, password, nonce, method, uri, nc, cnonce, qop);
            Assert.AreEqual(expectedResponse, digest);
        }
    }
}
