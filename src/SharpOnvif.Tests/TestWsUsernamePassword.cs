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
