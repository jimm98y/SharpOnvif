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
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SharpOnvifCommon.Xml;
using SharpOnvifCommon.Onvif;
using SharpOnvifServer.DeviceMgmt;
using SharpOnvifServer.Media;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The generated types carry their own XML reading and writing rather than being serialized
    /// by reflection. They also still carry the XmlSerializer attributes that describe the same
    /// mapping, which makes the two independent descriptions of one format comparable: these
    /// tests serialize an object both ways and require the results to agree.
    /// <para>
    /// That is what keeps the hand-written serializers honest. A mistake in the emitted read or
    /// write code shows up here as a disagreement with what XmlSerializer makes of the very same
    /// attributes.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestGeneratedSerialization
    {
        private const string Tds = "http://www.onvif.org/ver10/device/wsdl";
        private const string Trt = "http://www.onvif.org/ver10/media/wsdl";

        [TestMethod]
        public void SharesTheOnvifSchemaTypesBetweenClientAndServer()
        {
            // The Onvif schema is generated once into the common assembly rather than per
            // service, so a Profile is one CLR type no matter which side produced it. That is
            // what lets a value read by the client be handed to a server implementation
            // unchanged, and it is the property the whole deduplication rests on.
            Assert.AreSame(
                typeof(SharpOnvifCommon.Onvif.Profile),
                typeof(SharpOnvifCommon.Onvif.Profile));

            // The types a service declares for itself stay with that service on each side.
            Assert.AreNotSame(
                typeof(SharpOnvifClient.DeviceMgmt.GetDeviceInformationResponse),
                typeof(SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse));

            // And a schema type is reachable from both sides as the same type.
            var fromClient = typeof(SharpOnvifClient.Media.GetProfilesResponse)
                .GetProperty("Profiles").PropertyType.GetElementType();
            var fromServer = typeof(SharpOnvifServer.Media.GetProfilesResponse)
                .GetProperty("Profiles").PropertyType.GetElementType();

            Assert.AreSame(fromClient, fromServer, "both sides must name the same Profile type");
            Assert.AreSame(typeof(SharpOnvifCommon.Onvif.Profile), fromClient);
        }

        [TestMethod]
        public void WritesAFlatResponseLikeXmlSerializer()
        {
            var value = new GetDeviceInformationResponse
            {
                Manufacturer = "ACME",
                Model = "Vaultboy 9000",
                FirmwareVersion = "1.2.3",
                SerialNumber = "SN-000042",
                // Characters that have to be escaped, so encoding is compared too.
                HardwareId = "HW & <1>",
            };

            AssertRoundTrips(value, Tds, "GetDeviceInformationResponse");
        }

        [TestMethod]
        public void WritesNestedTypesAndEnumsLikeXmlSerializer()
        {
            var value = new GetSystemDateAndTimeResponse
            {
                SystemDateAndTime = new SystemDateTime
                {
                    DateTimeType = SetDateTimeType.NTP,
                    DaylightSavings = true,
                    UTCDateTime = new SharpOnvifCommon.Onvif.OnvifDateTime
                    {
                        Date = new Date { Year = 2026, Month = 9, Day = 12 },
                        Time = new Time { Hour = 21, Minute = 5, Second = 42 },
                    },
                },
            };

            AssertRoundTrips(value, Tds, "GetSystemDateAndTimeResponse");
        }

        [TestMethod]
        public void WritesAnInheritedTypeLikeXmlSerializer()
        {
            // VideoEncoderConfiguration extends ConfigurationEntity, so the base type's elements
            // have to be written first and its attribute alongside the derived type's.
            var value = new VideoEncoderConfiguration
            {
                Name = "H264",
                UseCount = 2,
                token = "venc0",
                Encoding = VideoEncoding.H264,
                Resolution = new SharpOnvifCommon.Onvif.VideoResolution { Width = 1920, Height = 1080 },
                Quality = 4.5f,
                SessionTimeout = "PT60S",
            };

            AssertRoundTrips(value, Trt, "Configuration");
        }

        [TestMethod]
        public void WritesArraysAndOptionalValueTypesLikeXmlSerializer()
        {
            var value = new GetProfilesResponse
            {
                Profiles = new[]
                {
                    // "fixed" is an optional bool, so it only appears when its companion
                    // Specified flag is set.
                    new Profile { token = "p0", Name = "MainStream", @fixed = true, fixedSpecified = true },
                    new Profile { token = "p1", Name = "SubStream" },
                },
            };

            AssertRoundTrips(value, Trt, "GetProfilesResponse");
        }

        [TestMethod]
        public void WritesAWrappedArrayLikeXmlSerializer()
        {
            // DegreeList is a tt:IntItems wrapper that collapses to int[], written as
            // <DegreeList><Items>0</Items>...</DegreeList>.
            var value = new RotateOptions
            {
                Mode = new[] { RotateMode.ON, RotateMode.OFF },
                DegreeList = new[] { 0, 90, 180, 270 },
            };

            AssertRoundTrips(value, Trt, "RotateOptions");
        }

        [TestMethod]
        public void ReadsBackWhatXmlSerializerWrote()
        {
            var value = new GetDeviceInformationResponse
            {
                Manufacturer = "ACME",
                Model = "Vaultboy 9000",
                FirmwareVersion = "1.2.3",
                SerialNumber = "SN-000042",
                HardwareId = "HW-1",
            };

            string xml = WriteWithXmlSerializer(value, Tds, "GetDeviceInformationResponse");
            var parsed = ReadWithGeneratedReader<GetDeviceInformationResponse>(xml);

            Assert.AreEqual("ACME", parsed.Manufacturer);
            Assert.AreEqual("Vaultboy 9000", parsed.Model);
            Assert.AreEqual("1.2.3", parsed.FirmwareVersion);
            Assert.AreEqual("SN-000042", parsed.SerialNumber);
            Assert.AreEqual("HW-1", parsed.HardwareId);
        }

        [TestMethod]
        public void ReadsElementsInAnyOrder()
        {
            // Devices do reorder a sequence. Matching by name rather than position means a
            // response that is out of order still deserializes.
            string xml =
                "<GetDeviceInformationResponse xmlns=\"" + Tds + "\">" +
                "<HardwareId>HW-1</HardwareId>" +
                "<Manufacturer>ACME</Manufacturer>" +
                "<SerialNumber>SN-1</SerialNumber>" +
                "</GetDeviceInformationResponse>";

            var parsed = ReadWithGeneratedReader<GetDeviceInformationResponse>(xml);

            Assert.AreEqual("ACME", parsed.Manufacturer);
            Assert.AreEqual("HW-1", parsed.HardwareId);
            Assert.AreEqual("SN-1", parsed.SerialNumber);
            Assert.IsNull(parsed.Model, "an element the device omitted stays unset");
        }

        [TestMethod]
        public void SkipsElementsTheSchemaDoesNotDescribe()
        {
            // Vendor extensions are common and must not fail the whole response.
            string xml =
                "<GetDeviceInformationResponse xmlns=\"" + Tds + "\">" +
                "<Manufacturer>ACME</Manufacturer>" +
                "<VendorSpecific><Nested attr=\"1\">x</Nested></VendorSpecific>" +
                "<Model>M</Model>" +
                "</GetDeviceInformationResponse>";

            var parsed = ReadWithGeneratedReader<GetDeviceInformationResponse>(xml);

            Assert.AreEqual("ACME", parsed.Manufacturer);
            Assert.AreEqual("M", parsed.Model, "reading has to continue past an unknown element");
        }

        // ------------------------------------------------------------------ helpers

        private static void AssertRoundTrips<T>(T value, string ns, string elementName)
            where T : OnvifContract, new()
        {
            string reflected = WriteWithXmlSerializer(value, ns, elementName);
            string generated = WriteWithGeneratedWriter(value, ns, elementName);

            Assert.AreEqual(Canonical(reflected), Canonical(generated),
                "the generated writer disagrees with the XmlSerializer attributes on the same type");

            // And what the reader makes of that XML has to write back out the same way.
            T parsed = ReadWithGeneratedReader<T>(reflected);
            Assert.AreEqual(Canonical(reflected), Canonical(WriteWithGeneratedWriter(parsed, ns, elementName)),
                "reading and writing are not inverses");
        }

        private static string WriteWithGeneratedWriter(OnvifContract value, string ns, string elementName)
        {
            var builder = new StringBuilder();
            using (XmlWriter xml = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                var writer = new OnvifXmlWriter(xml);
                writer.WriteStartElement(ns, elementName);
                writer.WriteContent(value);
                writer.WriteEndElement();
            }
            return builder.ToString();
        }

        private static string WriteWithXmlSerializer(object value, string ns, string elementName)
        {
            var serializer = new XmlSerializer(value.GetType(), new XmlRootAttribute(elementName) { Namespace = ns });
            var prefixes = new XmlSerializerNamespaces();
            prefixes.Add("", ns);

            var builder = new StringBuilder();
            using (XmlWriter xml = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                serializer.Serialize(xml, value, prefixes);
            }
            return builder.ToString();
        }

        private static T ReadWithGeneratedReader<T>(string xml) where T : OnvifContract, new()
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                reader.MoveToContent();
                return new OnvifXmlReader(reader).ReadElementObject(() => new T());
            }
        }

        /// <summary>
        /// Rewrites a document so only structure and values remain. The two writers choose
        /// different prefixes and declare namespaces in different places, neither of which
        /// changes what the XML means.
        /// </summary>
        private static string Canonical(string xml)
        {
            var document = new XmlDocument();
            document.LoadXml(xml);

            var builder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(builder, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                WriteCanonical(document.DocumentElement, writer);
            }
            return builder.ToString();
        }

        private static void WriteCanonical(XmlElement element, XmlWriter writer)
        {
            writer.WriteStartElement(element.LocalName, element.NamespaceURI);

            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (attribute.Prefix == "xmlns" || attribute.LocalName == "xmlns") continue;
                writer.WriteAttributeString(attribute.LocalName,
                    attribute.NamespaceURI.Length == 0 ? null : attribute.NamespaceURI, attribute.Value);
            }

            foreach (XmlNode child in element.ChildNodes)
            {
                if (child is XmlElement childElement) WriteCanonical(childElement, writer);
                else if (child is XmlText text) writer.WriteString(text.Value);
            }

            writer.WriteEndElement();
        }
    }
}
