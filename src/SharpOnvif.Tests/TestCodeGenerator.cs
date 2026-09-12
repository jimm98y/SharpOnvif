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
using System.Linq;
using SharpOnvif.CodeGen.Configuration;
using SharpOnvif.CodeGen.Generation;
using SharpOnvif.CodeGen.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The generator against a WSDL that is not Onvif.
    /// <para>
    /// Nothing in the compiler is specific to Onvif - it reads WSDL and XML Schema - and this is
    /// what keeps it that way. The fixture is a small banking service with an imported schema,
    /// an enumeration and a decimal, none of which appear anywhere in the Onvif specifications.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestCodeGenerator
    {
        private static string Fixture => Path.Combine(AppContext.BaseDirectory, "Wsdl", "bank.wsdl");

        private static string NewOutputDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "sharponvif-codegen-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        [TestMethod]
        public void GeneratesFromAWsdlThatIsNotOnvif()
        {
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output]);

                Assert.IsNotNull(options);
                var result = new WsdlCodeGenerator(options).Run();

                Assert.AreEqual(2, result.Operations, "GetBalance and Transfer");
                Assert.AreEqual(1, result.Services.Count);
                Assert.AreEqual("Bank", result.Services[0].Service, "the name comes from the file");

                // The imported schema is shared, so it is generated once of its own accord.
                Assert.AreEqual(2, result.SharedTypes, "Money and Currency");

                string service = Path.Combine(output, "Bank");
                Assert.IsTrue(File.Exists(Path.Combine(service, "DataContracts.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(service, "Client.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(service, "Service.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(output, "Schema", "DataContracts.cs")));

                string client = File.ReadAllText(Path.Combine(service, "Client.cs"));
                StringAssert.Contains(client, "namespace Example.Banking.Bank");
                StringAssert.Contains(client, "public partial class AccountsClient");

                // Both call styles, as for any other service.
                StringAssert.Contains(client, "GetBalanceAsync(GetBalanceRequest request");
                StringAssert.Contains(client, "GetBalanceAsync(string AccountId");

                // A type from the imported schema is referenced across the namespace boundary.
                StringAssert.Contains(client, "Example.Banking.Schema.Money Amount");

                string shared = File.ReadAllText(Path.Combine(output, "Schema", "DataContracts.cs"));
                StringAssert.Contains(shared, "namespace Example.Banking.Schema");
                StringAssert.Contains(shared, "public enum Currency");
                StringAssert.Contains(shared, "public partial class Money");
                StringAssert.Contains(shared, "public decimal Amount");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void WritesNothingWhenNothingChanged()
        {
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output]);

                int first = new WsdlCodeGenerator(options!).Run().FilesWritten;
                int second = new WsdlCodeGenerator(options!).Run().FilesWritten;

                Assert.AreEqual(4, first);
                Assert.AreEqual(0, second, "an unchanged run must leave the files alone");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void GeneratesOnlyTheSideThatWasAskedFor()
        {
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output, "--client"]);

                new WsdlCodeGenerator(options!).Run();

                string service = Path.Combine(output, "Bank");
                Assert.IsTrue(File.Exists(Path.Combine(service, "Client.cs")));
                Assert.IsFalse(File.Exists(Path.Combine(service, "Service.cs")), "--client asked for no service");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void FallsBackToThisRepositoryWhenGivenNoArguments()
        {
            // No --wsdl means the caller wants this repository's own bindings regenerated.
            Assert.IsNull(CommandLine.Parse([]));
        }

        [TestMethod]
        public void NamesAServiceFromTheArgumentWhenGiven()
        {
            var options = CommandLine.Parse(
                ["--wsdl", "Accounts=" + Fixture, "--namespace", "Example", "--out", "out"]);

            Assert.AreEqual("Accounts", options!.Services.Single().Name);
        }

        [TestMethod]
        public void DefaultsTheSharedNamespaceUnderTheRoot()
        {
            var options = CommandLine.Parse(["--wsdl", Fixture, "--namespace", "Example", "--out", "out"]);

            Assert.AreEqual("Example.Schema", options!.SharedNamespace);
        }

        [TestMethod]
        public void RejectsArgumentsItCannotActOn()
        {
            Assert.ThrowsExactly<SchemaException>(
                () => CommandLine.Parse(["--wsdl", Fixture]),
                "a namespace and an output directory are both required");

            Assert.ThrowsExactly<SchemaException>(
                () => CommandLine.Parse(["--nonsense"]));

            Assert.ThrowsExactly<SchemaException>(
                () => CommandLine.Parse(["--wsdl"]),
                "an option that takes a value has to be given one");
        }
    }
}
