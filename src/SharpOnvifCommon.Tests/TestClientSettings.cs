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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using SharpOnvifCommon;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Copying a client's settings.
    /// </summary>
    /// <remarks>
    /// A client that needs one thing different - a longer timeout for a long poll - is built from
    /// a copy, and a copy written as a list of members goes stale in a way nothing notices: what
    /// it forgets is not missing, it is silently whatever the default happens to be. This is why
    /// the copy is checked by reflection rather than by naming the members again here, which would
    /// be a second list to forget the same thing.
    /// </remarks>
    [TestClass]
    public sealed class TestClientSettings
    {
        [TestMethod]
        public void CopiesEveryLastSetting()
        {
            var original = new OnvifClientSettings();
            var properties = Settable().ToList();

            Assert.IsTrue(properties.Count >= 11, "the settings lost a property, or this found none");

            foreach (PropertyInfo property in properties)
                property.SetValue(original, Distinctive(property));

            var copy = new OnvifClientSettings(original);

            foreach (PropertyInfo property in properties)
            {
                Assert.AreEqual(
                    property.GetValue(original), property.GetValue(copy),
                    $"{property.Name} did not survive the copy - add it to the copy constructor");
            }
        }

        [TestMethod]
        public void RefusesToCopyNothing()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new OnvifClientSettings(null));
        }

        private static IEnumerable<PropertyInfo> Settable()
        {
            return typeof(OnvifClientSettings)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);
        }

        /// <summary>
        /// A value for a property that is not the value it already has, so that a member the copy
        /// forgets reads back as the default rather than as what was set.
        /// </summary>
        private static object Distinctive(PropertyInfo property)
        {
            Type type = property.PropertyType;

            if (type == typeof(TimeSpan)) return TimeSpan.FromSeconds(17);
            if (type == typeof(bool)) return !(bool)property.GetValue(new OnvifClientSettings());
            if (type == typeof(long)) return 4242L;
            if (type == typeof(NetworkCredential)) return new NetworkCredential("someone", "something");
            if (type == typeof(ILog)) return new DefaultOnvifLogger();
            if (type == typeof(IClientAuthentication)) return new OnvifClientAuthentication(DigestAuthentication.None);
            if (type == typeof(IMessageCodec)) return new SoapMessageCodec();
            if (type == typeof(HttpMessageHandler)) return new HttpClientHandler();
            if (type == typeof(HttpClient)) return new HttpClient();
            if (type == typeof(IEnumerable<XmlNamespaceDeclaration>))
                return new[] { new XmlNamespaceDeclaration("x", "urn:example") };

            throw new AssertFailedException(
                $"{property.Name} is a {type.Name}, which this test does not know how to tell apart. " +
                "Teach it, rather than leaving the property unchecked.");
        }
    }
}
