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

using System.Collections.Generic;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The settings two sides agree on, and the questions worth asking of them.
    /// <para>
    /// The settings answer none of those themselves: the same object describes a device's offer
    /// and a client's understanding, so anything it did would be one side's behaviour living in
    /// both sides' description.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestAuthenticationSettings
    {
        private const string GetSystemDateAndTime =
            "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime";

        [TestMethod]
        public void DescribesWithoutDoing()
        {
            // What a client authenticates with is not the description of what it agreed to.
            Assert.IsFalse(
                typeof(IClientAuthentication).IsAssignableFrom(typeof(OnvifAuthenticationSettings)),
                "the description has grown behaviour again");

            Assert.IsTrue(typeof(IClientAuthentication).IsAssignableFrom(typeof(OnvifClientAuthentication)));
        }

        [TestMethod]
        public void KnowsWhichActionsNeedNoCredentials()
        {
            var settings = new OnvifAuthenticationSettings();

            Assert.IsTrue(settings.IsPreAuth(GetSystemDateAndTime));
            Assert.IsFalse(settings.IsPreAuth("http://www.onvif.org/ver10/device/wsdl/SetUser"));
        }

        [TestMethod]
        public void AsksNothingOfWhatIsNotThere()
        {
            // The question gets asked of settings a caller assembled, so none of these is an
            // error - each is an honest "no".
            OnvifAuthenticationSettings missing = null;

            Assert.IsFalse(missing.IsPreAuth(GetSystemDateAndTime));
            Assert.IsFalse(new OnvifAuthenticationSettings { PreAuthActions = null }.IsPreAuth(GetSystemDateAndTime));
            Assert.IsFalse(new OnvifAuthenticationSettings().IsPreAuth(null));
            Assert.IsFalse(missing.Offers(DigestAuthentication.HttpDigest));
        }

        [TestMethod]
        public void AnActionRemovedIsAnActionAuthenticated()
        {
            // Some devices demand credentials for these anyway, and taking one off the list is how
            // a client is told so.
            var settings = new OnvifAuthenticationSettings();
            settings.PreAuthActions.Remove(GetSystemDateAndTime);

            Assert.IsFalse(settings.IsPreAuth(GetSystemDateAndTime));
        }

        [TestMethod]
        public void SaysWhichSchemesItOffers()
        {
            var both = new OnvifAuthenticationSettings(
                DigestAuthentication.HttpDigest | DigestAuthentication.WsUsernameToken);

            Assert.IsTrue(both.Offers(DigestAuthentication.HttpDigest));
            Assert.IsTrue(both.Offers(DigestAuthentication.WsUsernameToken));

            var digestOnly = new OnvifAuthenticationSettings(DigestAuthentication.HttpDigest);

            Assert.IsTrue(digestOnly.Offers(DigestAuthentication.HttpDigest));
            Assert.IsFalse(digestOnly.Offers(DigestAuthentication.WsUsernameToken));
        }

        [TestMethod]
        public void CarriesTheSettingsItWasBuiltWith()
        {
            var settings = new OnvifAuthenticationSettings(DigestAuthentication.HttpDigest)
            {
                HttpDigestAlgorithms = new List<string> { "SHA-256" },
            };

            var authentication = new OnvifClientAuthentication(settings);

            Assert.AreSame(settings, authentication.Settings);
            Assert.IsNotNull(new OnvifClientAuthentication((OnvifAuthenticationSettings)null).Settings,
                "a client would throw on the next read");
        }
    }
}
