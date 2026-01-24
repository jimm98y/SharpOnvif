using SharpOnvifCommon.Security;

namespace SharpOnvif.Tests
{
    [TestClass]
    public sealed class TestRFC2069
    {
        // According to the errata (https://www.rfc-editor.org/errata/rfc2069) the response e966c932a9242554e42c8ee200cec7f6 is incorrect, it should be 1949323746fe6a43ef61f9606e7febea
        [DataRow("Mufasa", "CircleOfLife", "testrealm@host.com", "/dir/index.html", "dcd98b7102dd2f0e8b11d0f600bfb0c093", "GET", "5ccc069c403ebaf9f0171e9517f40e41", "1949323746fe6a43ef61f9606e7febea")]
        [TestMethod]
        public void TestDigest(string userName, string password, string realm, string uri, string nonce, string method, string opaque, string expectedResponse)
        {
            string digest = DigestHelpers.CreateWebDigestRFC2069(userName, realm, password, nonce, method, uri);
            Assert.AreEqual(expectedResponse, digest);
        }
    }
}
