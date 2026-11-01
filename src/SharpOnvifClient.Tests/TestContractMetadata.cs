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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using SharpOnvifClient.DeviceMgmt;
using SharpOnvifCommon.Onvif;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The description every generated contract and client carries of itself, which lets a tool
    /// list, read, write and call any of them without reflection.
    /// <para>
    /// Reflection is what these tests check it against: the description has to say what the
    /// properties, the XmlSerializer attributes and the methods say, for every generated type.
    /// And it has to cost nothing until somebody asks for it.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestContractMetadata
    {
        private const string Endpoint = "http://192.168.1.10/onvif/device_service";

        private static IEnumerable<Type> Contracts() =>
            new[] { typeof(XmlContract).Assembly, typeof(DeviceClient).Assembly }
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(XmlContract).IsAssignableFrom(t));

        private static IEnumerable<Type> Clients() =>
            typeof(DeviceClient).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(SoapClientBase).IsAssignableFrom(t));

        /// <summary>The properties a contract is made of, inherited ones first, without the XxxSpecified flags.</summary>
        private static List<PropertyInfo> Properties(Type type)
        {
            var levels = new List<Type>();
            for (var level = type; level != null && level != typeof(XmlContract); level = level.BaseType) levels.Insert(0, level);

            var all = new List<PropertyInfo>();
            foreach (var level in levels)
            {
                var declared = level.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    // the description itself: XmlMembers, XmlRootElement and the like
                    .Where(p => typeof(XmlContract).GetProperty(p.Name) == null)
                    .OrderBy(p => p.MetadataToken)
                    .ToList();
                all.AddRange(declared.Where(p => !(p.Name.EndsWith("Specified", StringComparison.Ordinal) &&
                    declared.Any(o => o.Name + "Specified" == p.Name))));
            }
            return all;
        }

        [TestMethod]
        public void DescribesEveryPropertyOfEveryContract()
        {
            int contracts = 0;
            foreach (var type in Contracts())
            {
                var instance = (XmlContract)Activator.CreateInstance(type);
                var expected = Properties(type).Select(p => p.Name).ToList();
                var described = instance.XmlMembers.Select(m => m.Name).ToList();

                CollectionAssert.AreEqual(expected, described, $"{type.FullName} is described as it is not");
                Assert.AreEqual(type, instance.CreateXmlInstance().GetType(), $"{type.FullName} makes an instance of another type");
                contracts++;
            }
            Assert.IsTrue(contracts > 2000, $"only {contracts} contracts were found");
        }

        [TestMethod]
        public void ReadsAndWritesEveryMemberOfEveryContract()
        {
            foreach (var type in Contracts())
            {
                var instance = (XmlContract)Activator.CreateInstance(type);
                var properties = Properties(type).ToDictionary(p => p.Name);

                foreach (var member in instance.XmlMembers)
                {
                    var property = properties[member.Name];
                    string where = $"{type.FullName}.{member.Name}";

                    Assert.AreEqual(property.PropertyType, member.Type, $"{where} has another type");
                    // a byte[] is one value, base64 on the wire, not an array of items
                    Assert.AreEqual(member.IsArray ? member.ItemType.MakeArrayType() : member.ItemType, property.PropertyType,
                        $"{where} has another item type");

                    object value = member.CreateValue();
                    if (value != null)
                    {
                        Assert.IsInstanceOfType(value, property.PropertyType, $"{where} makes a value it cannot hold");
                        member.SetValue(instance, value);
                        Assert.AreEqual(value, property.GetValue(instance), $"{where} is written somewhere else");
                        Assert.AreEqual(value, member.GetValue(instance), $"{where} is read from somewhere else");
                    }

                    if (member.IsArray)
                    {
                        Array array = member.CreateArray(3);
                        Assert.IsInstanceOfType(array, property.PropertyType, $"{where} makes an array it cannot hold");
                        Assert.AreEqual(3, array.Length);
                        object item = member.CreateItem();
                        if (item != null) Assert.IsInstanceOfType(item, member.ItemType, $"{where} makes an item it cannot hold");
                    }

                    var specified = type.GetProperty(member.Name + "Specified");
                    Assert.AreEqual(specified != null, member.HasSpecified, $"{where} has a Specified flag or not wrongly");
                    if (specified != null)
                    {
                        member.SetSpecified(instance, true);
                        Assert.IsTrue((bool)specified.GetValue(instance), $"{where}Specified is written somewhere else");
                        specified.SetValue(instance, false);
                        Assert.IsFalse(member.GetSpecified(instance), $"{where}Specified is read from somewhere else");
                    }
                }
            }
        }

        [TestMethod]
        public void NamesEveryMemberAsXmlSerializerWould()
        {
            foreach (var type in Contracts())
            {
                var properties = Properties(type).ToDictionary(p => p.Name);
                foreach (var member in ((XmlContract)Activator.CreateInstance(type)).XmlMembers)
                {
                    var property = properties[member.Name];
                    string where = $"{type.FullName}.{member.Name}";

                    switch (member.Kind)
                    {
                        case XmlMemberKind.Attribute:
                            var attribute = property.GetCustomAttribute<XmlAttributeAttribute>();
                            Assert.AreEqual(string.IsNullOrEmpty(attribute.AttributeName) ? property.Name : attribute.AttributeName, member.XmlName, where);
                            break;

                        case XmlMemberKind.Element:
                            var array = property.GetCustomAttribute<XmlArrayAttribute>();
                            var element = property.GetCustomAttribute<XmlElementAttribute>();
                            string name = array != null ? array.ElementName : element.ElementName;
                            Assert.AreEqual(string.IsNullOrEmpty(name) ? property.Name : name, member.XmlName, where);
                            Assert.AreEqual(array != null ? array.Order : element.Order, member.Order, $"{where} is ordered differently");
                            break;

                        case XmlMemberKind.Choice:
                            var options = property.GetCustomAttributes<XmlElementAttribute>().Select(e => e.ElementName).OrderBy(n => n).ToList();
                            var described = member.Choices.Where(c => !c.IsWildcard).Select(c => c.ElementName).OrderBy(n => n).ToList();
                            CollectionAssert.AreEqual(options, described, $"{where} offers other branches");
                            break;
                    }
                }
            }
        }

        [TestMethod]
        public void ListsTheValuesOfEveryEnum()
        {
            int enums = 0;
            foreach (var type in Contracts())
            {
                foreach (var member in ((XmlContract)Activator.CreateInstance(type)).XmlMembers.Where(m => m.ValueKind == XmlValueKind.Enum))
                {
                    var fields = member.ItemType.GetFields(BindingFlags.Public | BindingFlags.Static);
                    CollectionAssert.AreEqual(
                        fields.Select(f => f.GetValue(null)).ToList(),
                        member.EnumValues.Select(v => v.Value).ToList(),
                        $"{type.FullName}.{member.Name} lists other values");
                    CollectionAssert.AreEqual(
                        fields.Select(f => f.GetCustomAttribute<XmlEnumAttribute>()?.Name ?? f.Name).ToList(),
                        member.EnumValues.Select(v => v.XmlName).ToList(),
                        $"{type.FullName}.{member.Name} names its values differently");
                    enums++;
                }
            }
            Assert.IsTrue(enums > 100, $"only {enums} enum members were found");
        }

        [TestMethod]
        public void ListsEveryOperationOfEveryClient()
        {
            int operations = 0;
            foreach (var type in Clients())
            {
                var expected = type.GetInterfaces()
                    .Where(i => i.Namespace == type.Namespace)
                    .SelectMany(i => i.GetMethods().Select(m => (Port: i.Name, Method: m)))
                    // the overload that takes the request: the other takes its members, which may be one contract too
                    .Where(x => x.Method.GetParameters() is var p && p.Length == 2 && p[0].Name == "request" &&
                                typeof(XmlContract).IsAssignableFrom(p[0].ParameterType) && p[1].ParameterType == typeof(CancellationToken))
                    .ToList();

                var described = (IReadOnlyList<SoapOperation>)type.GetProperty("Operations", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                Assert.AreEqual(expected.Count, described.Count, $"{type.Name} lists another number of operations");

                foreach (var (port, method) in expected)
                {
                    string name = method.Name.Substring(0, method.Name.Length - "Async".Length);
                    var operation = described.Single(o => o.Name == name);
                    Assert.AreEqual(port, operation.Port, $"{type.Name}.{name} belongs to another port");
                    Assert.AreEqual(method.GetParameters()[0].ParameterType, operation.RequestType, $"{type.Name}.{name} takes another request");
                    Assert.AreEqual(operation.RequestType, operation.CreateRequest().GetType(), $"{type.Name}.{name} makes another request");
                    var response = method.ReturnType.IsGenericType ? method.ReturnType.GetGenericArguments()[0] : null;
                    Assert.AreEqual(response, operation.ResponseType, $"{type.Name}.{name} answers with another response");
                    operations++;
                }
            }
            Assert.AreEqual(627, operations, "every operation of every service is listed");
        }

        [TestMethod]
        public void CreatesAClientFromItsType()
        {
            using (var client = DeviceClient.Create(Endpoint, new OnvifClientSettings()))
            {
                Assert.IsInstanceOfType<DeviceClient>(client);
            }
        }

        /// <summary>A transport that answers every request with one SOAP response, and keeps the requests.</summary>
        private sealed class AnsweringTransport : HttpMessageHandler
        {
            private readonly string _envelope;

            public AnsweringTransport(string envelope) { _envelope = envelope; }

            public List<string> Requests { get; } = new List<string>();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(await request.Content.ReadAsStringAsync(cancellationToken));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_envelope, Encoding.UTF8, "application/soap+xml"),
                };
            }
        }

        [TestMethod]
        public async Task CallsAnOperationThroughItsDescription()
        {
            var transport = new AnsweringTransport(
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body>" +
                "<tds:GetHostnameResponse xmlns:tds=\"http://www.onvif.org/ver10/device/wsdl\" xmlns:tt=\"http://www.onvif.org/ver10/schema\">" +
                "<tds:HostnameInformation><tt:FromDHCP>false</tt:FromDHCP><tt:Name>camera-1</tt:Name></tds:HostnameInformation>" +
                "</tds:GetHostnameResponse></s:Body></s:Envelope>");

            using (var client = DeviceClient.Create(Endpoint, new OnvifClientSettings { Transport = transport }))
            {
                var operation = DeviceClient.Operations.Single(o => o.Name == "GetHostname");
                var response = await operation.InvokeAsync(client, operation.CreateRequest());

                Assert.IsInstanceOfType<GetHostnameResponse>(response);
                Assert.AreEqual("camera-1", ((GetHostnameResponse)response).HostnameInformation.Name);
                StringAssert.Contains(transport.Requests.Single(), "GetHostname", "the request went out as the method sends it");
            }
        }

        [TestMethod]
        public void WritesTheElementARequestIsSentAs()
        {
            var request = new SetHostnameRequest("camera-1");
            Assert.AreEqual(new XmlQualifiedName("SetHostname", "http://www.onvif.org/ver10/device/wsdl"), request.XmlRootElement);
            Assert.IsNull(new HostnameInformation().XmlRootElement, "only a message is written as an element of its own");
            Assert.AreEqual(new XmlQualifiedName("HostnameInformation", "http://www.onvif.org/ver10/schema"), new HostnameInformation().XmlType);
        }

        [TestMethod]
        public void BuildsNothingUntilAsked()
        {
            // A static constructor would run on the first touch of the type - an instance made, a
            // response read - and is what the tables are written to do without.
            foreach (var type in Contracts().Concat(Clients()))
                Assert.IsNull(type.TypeInitializer, $"{type.FullName} has a static constructor");

            var members = typeof(VideoEncoderConfiguration).GetField("s_xmlMembers", BindingFlags.NonPublic | BindingFlags.Static);
            members.SetValue(null, null);

            // Made, filled and written: none of it asks for the description.
            var configuration = new VideoEncoderConfiguration { Name = "Main", Quality = 5, Resolution = new VideoResolution { Width = 640, Height = 480 } };
            using (var text = new StringWriter())
            using (var xml = XmlWriter.Create(text))
            {
                xml.WriteStartElement("Configuration");
                new OnvifXmlWriter(xml).WriteContent(configuration);
                xml.WriteEndElement();
            }
            Assert.IsNull(members.GetValue(null), "making and writing a contract built its description");

            var first = configuration.XmlMembers;
            Assert.IsNotNull(members.GetValue(null), "asking built nothing");
            Assert.AreSame(first, VideoEncoderConfiguration.GetXmlMembers(), "the description is built once");

            var operations = typeof(DeviceClient).GetField("s_operations", BindingFlags.NonPublic | BindingFlags.Static);
            operations.SetValue(null, null);
            using (DeviceClient.Create(Endpoint, new OnvifClientSettings())) { }
            Assert.IsNull(operations.GetValue(null), "making a client built its operations");
            Assert.AreSame(DeviceClient.Operations, DeviceClient.Operations, "the operations are built once");
        }
    }
}
