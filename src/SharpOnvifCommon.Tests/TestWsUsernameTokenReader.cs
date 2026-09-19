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
using System.IO;
using System.Xml;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Reading the credentials a client put in a SOAP Security header. This is what the server
    /// authenticates against, so what it accepts and what it refuses is the shape of the device's
    /// front door: everything a real camera would let through has to survive, and nothing that
    /// cannot be checked may come back looking like a credential.
    /// </summary>
    [TestClass]
    public sealed class TestWsUsernameTokenReader
    {
        private const string Wsse = WsUsernameToken.SecurityExtNamespace;
        private const string Wsu = WsUsernameToken.SecurityUtilityNamespace;

        private static UsernameToken Read(string xml)
        {
            using (var reader = XmlReader.Create(new StringReader(xml)))
            {
                return WsUsernameToken.Read(reader);
            }
        }

        private static string Envelope(string header)
        {
            return
                "<s:Envelope xmlns:s=\"" + OnvifXmlNamespaces.SoapEnvelope + "\">" +
                "<s:Header>" + header + "</s:Header>" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\" /></s:Body>" +
                "</s:Envelope>";
        }

        private static string SecurityHeader(
            string username = "admin",
            string password = "tuOSpGlFlIXsozq4HFNeeGeFLEI=",
            string nonce = "LKqI6G/AikKCQrN0zqZFlg==",
            string created = "2010-09-16T07:50:45Z")
        {
            return
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\" xmlns:wsu=\"" + Wsu + "\">" +
                "<wsse:UsernameToken>" +
                "<wsse:Username>" + username + "</wsse:Username>" +
                "<wsse:Password Type=\"http://docs.oasis-open.org/wss/2004/01/" +
                "oasis-200401-wss-username-token-profile-1.0#PasswordDigest\">" + password + "</wsse:Password>" +
                "<wsse:Nonce EncodingType=\"http://docs.oasis-open.org/wss/2004/01/" +
                "oasis-200401-wss-soap-message-security-1.0#Base64Binary\">" + nonce + "</wsse:Nonce>" +
                "<wsu:Created>" + created + "</wsu:Created>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";
        }

        [TestMethod]
        public void ReadsEveryPartOfATokenACameraWouldSend()
        {
            // All four values in one test on purpose: each is read by moving the reader off the
            // element that carried it, so reading one and losing the next is the mistake this
            // pins down. A test per element would not catch it.
            UsernameToken token = Read(Envelope(SecurityHeader()));

            Assert.IsNotNull(token, "the token was not found in the header");
            Assert.AreEqual("admin", token.Username);
            Assert.AreEqual("tuOSpGlFlIXsozq4HFNeeGeFLEI=", token.Password.Text);
            Assert.AreEqual("LKqI6G/AikKCQrN0zqZFlg==", token.Nonce.Text);
            Assert.AreEqual("2010-09-16T07:50:45Z", token.Created);
        }

        [TestMethod]
        public void ReadsTheTypeAndEncodingAttributes()
        {
            UsernameToken token = Read(Envelope(SecurityHeader()));

            StringAssert.EndsWith(token.Password.Type, "#PasswordDigest");
            StringAssert.EndsWith(token.Nonce.EncodingType, "#Base64Binary");
        }

        [TestMethod]
        public void ReadsWhatTheLibrarysOwnWriterProduces()
        {
            // The two sides of one header, so a change to either that the other does not follow
            // shows up here rather than as a camera that stops authenticating.
            string header = SoapEnvelope.Write(
                null,
                writer => WsUsernameToken.Write(writer, "operator", "secret", TimeSpan.Zero),
                writer => writer.WriteEmptyElement("http://www.onvif.org/ver10/device/wsdl", "GetDeviceInformation"));

            UsernameToken token = Read(header);

            Assert.IsNotNull(token);
            Assert.AreEqual("operator", token.Username);
            Assert.AreEqual(
                WsDigestAuthentication.CreateSoapDigest(token.Nonce.Text, token.Created, "secret"),
                token.Password.Text,
                "the digest the writer computed is not the one the reader read back");
        }

        [TestMethod]
        public void ReadsATokenWhoseElementsArriveInAnotherOrder()
        {
            // The profile lists them in an order, and devices do not always keep to it.
            string header =
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\" xmlns:wsu=\"" + Wsu + "\">" +
                "<wsse:UsernameToken>" +
                "<wsu:Created>2010-09-16T07:50:45Z</wsu:Created>" +
                "<wsse:Nonce>LKqI6G/AikKCQrN0zqZFlg==</wsse:Nonce>" +
                "<wsse:Password>digest</wsse:Password>" +
                "<wsse:Username>admin</wsse:Username>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";

            UsernameToken token = Read(Envelope(header));

            Assert.IsNotNull(token);
            Assert.AreEqual("admin", token.Username);
            Assert.AreEqual("digest", token.Password.Text);
            Assert.AreEqual("LKqI6G/AikKCQrN0zqZFlg==", token.Nonce.Text);
            Assert.AreEqual("2010-09-16T07:50:45Z", token.Created);
        }

        [TestMethod]
        public void ReadsPastAnElementTheProfileDoesNotDescribe()
        {
            // Vendor extensions sit in this header, and one must not cost the request its
            // credentials. The extension is given children so that skipping it has to skip a
            // subtree rather than a single element.
            string header =
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\" xmlns:wsu=\"" + Wsu + "\">" +
                "<wsse:UsernameToken>" +
                "<wsse:Username>admin</wsse:Username>" +
                "<Vendor xmlns=\"urn:acme\"><Nested attr=\"1\">x</Nested></Vendor>" +
                "<wsse:Password>digest</wsse:Password>" +
                "<wsse:Nonce>LKqI6G/AikKCQrN0zqZFlg==</wsse:Nonce>" +
                "<wsu:Created>2010-09-16T07:50:45Z</wsu:Created>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";

            UsernameToken token = Read(Envelope(header));

            Assert.IsNotNull(token);
            Assert.AreEqual("admin", token.Username);
            Assert.AreEqual("digest", token.Password.Text, "reading has to continue past the extension");
            Assert.AreEqual("2010-09-16T07:50:45Z", token.Created);
        }

        [TestMethod]
        public void FindsATokenUnderOtherHeadersAndOtherSecurityContent()
        {
            // The Security header is rarely the only one, and rarely holds only the token.
            string header =
                "<a:Action xmlns:a=\"http://www.w3.org/2005/08/addressing\">urn:GetDeviceInformation</a:Action>" +
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\" xmlns:wsu=\"" + Wsu + "\">" +
                "<wsu:Timestamp><wsu:Created>2010-09-16T07:50:45Z</wsu:Created></wsu:Timestamp>" +
                "<wsse:UsernameToken>" +
                "<wsse:Username>admin</wsse:Username>" +
                "<wsse:Password>digest</wsse:Password>" +
                "<wsse:Nonce>LKqI6G/AikKCQrN0zqZFlg==</wsse:Nonce>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";

            UsernameToken token = Read(Envelope(header));

            Assert.IsNotNull(token);
            Assert.AreEqual("admin", token.Username);
            Assert.AreEqual("digest", token.Password.Text);
            Assert.IsNull(token.Created, "the Timestamp's Created belongs to the timestamp, not to the token");
        }

        [TestMethod]
        public void LeavesCreatedUnsetWhenItIsNotInTheUtilityNamespace()
        {
            // Created carries the replay window, so reading one that is not the token's own would
            // be worse than reading none: the server would check the wrong timestamp.
            string header =
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\">" +
                "<wsse:UsernameToken>" +
                "<wsse:Username>admin</wsse:Username>" +
                "<wsse:Password>digest</wsse:Password>" +
                "<wsse:Nonce>LKqI6G/AikKCQrN0zqZFlg==</wsse:Nonce>" +
                "<wsse:Created>2010-09-16T07:50:45Z</wsse:Created>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";

            UsernameToken token = Read(Envelope(header));

            Assert.IsNotNull(token);
            Assert.IsNull(token.Created);
        }

        [TestMethod]
        public void ReturnsNothingForARequestCarryingNoHeaderAtAll()
        {
            string xml =
                "<s:Envelope xmlns:s=\"" + OnvifXmlNamespaces.SoapEnvelope + "\">" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\" /></s:Body>" +
                "</s:Envelope>";

            Assert.IsNull(Read(xml));
        }

        [TestMethod]
        public void ReturnsNothingForAHeaderWithoutASecurityElement()
        {
            string header =
                "<a:Action xmlns:a=\"http://www.w3.org/2005/08/addressing\">urn:GetDeviceInformation</a:Action>";

            Assert.IsNull(Read(Envelope(header)));
        }

        [TestMethod]
        public void ReturnsNothingForASecurityHeaderCarryingNoToken()
        {
            string header =
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\" xmlns:wsu=\"" + Wsu + "\">" +
                "<wsu:Timestamp><wsu:Created>2010-09-16T07:50:45Z</wsu:Created></wsu:Timestamp>" +
                "</wsse:Security>";

            Assert.IsNull(Read(Envelope(header)));
        }

        [TestMethod]
        public void ReturnsNothingForADocumentThatIsNotASoapEnvelope()
        {
            Assert.IsNull(Read("<html><body>not a device</body></html>"));
        }

        [TestMethod]
        public void LeavesTheMissingPartsOfAnIncompleteTokenUnset()
        {
            // The caller decides what an unusable token means; the reader's part is to report
            // what was there rather than to invent a password out of an absent element.
            string header =
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\">" +
                "<wsse:UsernameToken><wsse:Username>admin</wsse:Username></wsse:UsernameToken>" +
                "</wsse:Security>";

            UsernameToken token = Read(Envelope(header));

            Assert.IsNotNull(token);
            Assert.AreEqual("admin", token.Username);
            Assert.IsNull(token.Password);
            Assert.IsNull(token.Nonce);
        }

        [TestMethod]
        public void TellsAnEmptyPasswordApartFromAnAbsentOne()
        {
            string header =
                "<wsse:Security xmlns:wsse=\"" + Wsse + "\">" +
                "<wsse:UsernameToken>" +
                "<wsse:Username>admin</wsse:Username>" +
                "<wsse:Password />" +
                "<wsse:Nonce>LKqI6G/AikKCQrN0zqZFlg==</wsse:Nonce>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";

            UsernameToken token = Read(Envelope(header));

            Assert.IsNotNull(token.Password, "an element that is there is not an element that is absent");
            Assert.AreEqual(string.Empty, token.Password.Text);
            Assert.AreEqual("LKqI6G/AikKCQrN0zqZFlg==", token.Nonce.Text, "the element after an empty one is still read");
        }

        [TestMethod]
        public void IgnoresATokenInANamespaceThatIsNotWsSecuritys()
        {
            // The namespace is what says these elements mean what this code reads them as. One
            // that says otherwise is not a credential, and must not be read as though it were.
            string header =
                "<wsse:Security xmlns:wsse=\"urn:acme:security\">" +
                "<wsse:UsernameToken>" +
                "<wsse:Username>admin</wsse:Username>" +
                "<wsse:Password>digest</wsse:Password>" +
                "</wsse:UsernameToken>" +
                "</wsse:Security>";

            Assert.IsNull(Read(Envelope(header)));
        }

        [TestMethod]
        public void RefusesToReadFromNothing()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => WsUsernameToken.Read(null));
        }
    }
}
