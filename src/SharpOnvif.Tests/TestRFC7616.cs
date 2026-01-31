using SharpOnvifCommon.Security;

namespace SharpOnvif.Tests
{
    [TestClass]
    public sealed class TestRFC7616
    {
        [DataRow("MD5", "Mufasa", "Circle of Life", "http-auth@example.org", "/dir/index.html", "7ypf/xlj9XXwfDPEoM4URrv/xwf94BcCAzFZH4GiTo0v", "GET", "FQhe/qaU925kfnzjCev0ciny7QMkPqMAFRtzCUYo5tdS", 1, "f2/wE4q74E6zIJEtWaHKaf5wv/H5QzzpXusqGemxURZJ", "auth", "8ca523f5e9506fed4657c9700eebdbec")]
        [DataRow("SHA-256", "Mufasa", "Circle of Life", "http-auth@example.org", "/dir/index.html", "7ypf/xlj9XXwfDPEoM4URrv/xwf94BcCAzFZH4GiTo0v", "GET", "FQhe/qaU925kfnzjCev0ciny7QMkPqMAFRtzCUYo5tdS", 1, "f2/wE4q74E6zIJEtWaHKaf5wv/H5QzzpXusqGemxURZJ", "auth", "753927fa0e85d155564e2e272a28d1802ca10daf4496794697cf8db5856cb6c1")]
        [DataRow("SHA-512-256", "J\u00E4s\u00F8n Doe", "Secret, or not?", "api@example.org", "/doe.json", "5TsQWLVdgBdmrQ0XsxbDODV+57QdFR34I9HAbC/RVvkK", "GET", "HRPCssKJSGjCrkzDg8OhwpzCiGPChXYjwrI2QmXDnsOS", 1, "NTg6RKcb9boFIAS3KrFK9BGeh+iDa/sm6jUMp2wds69v", "auth", "ae66e67d6b427bd3f120414a82e4acff38e8ecd9101d6c861229025f607a79dd")]
        [TestMethod]
        public void TestDigest(string algorithm, string userName, string password, string realm, string uri, string nonce, string method, string opaque, int nc, string cnonce, string qop, string expectedResponse)
        {
            string digest = HttpDigestAuthentication.CreateWebDigestRFC2617(algorithm, userName, realm, password, false, nonce, method, uri, nc, cnonce, qop);
            Assert.AreEqual(expectedResponse, digest);
        }

        [DataRow("SHA-512-256", "J\u00E4s\u00F8n Doe", "api@example.org", "488869477bf257147b804c45308cd62ac4e25eb717b12b298c79e62dcea254ec")]
        [TestMethod]
        public void TestHashedUserName(string algorithm, string userName, string realm, string expectedResult)
        {
            string hashedUserName = HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, userName, realm);
            Assert.AreEqual(expectedResult, hashedUserName);
        }
    }
}
