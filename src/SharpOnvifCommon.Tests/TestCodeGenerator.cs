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
using WsdlGenerator.Configuration;
using WsdlGenerator.Emit;
using WsdlGenerator.Generation;
using WsdlGenerator.Xml;

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
                var result = new CodeGenerator(options).Run();

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

                int first = new CodeGenerator(options!).Run().FilesWritten;
                int second = new CodeGenerator(options!).Run().FilesWritten;

                Assert.AreEqual(
                    Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories).Length, first,
                    "the first run writes everything there is");
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

                new CodeGenerator(options!).Run();

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
        public void WritesTheRuntimeTheGeneratedCodeNeeds()
        {
            // The point of writing it rather than referencing it: a generated client is compiled
            // against its own runtime, so it names no library of ours at all. A client that still
            // mentioned SharpOnvif would not compile anywhere but in this repository.
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output]);

                new CodeGenerator(options!).Run();

                string runtime = Path.Combine(output, "Runtime");
                Assert.IsTrue(File.Exists(Path.Combine(runtime, "Soap", "OnvifClientBase.cs")),
                    "the base class the generated client derives from");
                Assert.IsTrue(File.Exists(Path.Combine(runtime, "Xml", "OnvifXmlReader.cs")));
                Assert.IsTrue(File.Exists(Path.Combine(runtime, "Soap", "IClientSettings.cs")));

                // Everything a client is built from: the client, the contracts it exchanges, the
                // shared schema and the runtime underneath them. The generated service is the one
                // exception, and deliberately so - it is routed by the ASP.NET Core dispatch in
                // SharpOnvifServer, which is a library rather than anything the generator writes.
                foreach (string file in Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file) == "Service.cs") continue;

                    string source = File.ReadAllText(file);

                    // The name in a licence header is not a dependency; a namespace is.
                    foreach (string assembly in new[] { "SharpOnvifCommon", "SharpOnvifClient", "SharpOnvifServer" })
                    {
                        Assert.IsFalse(source.Contains(assembly, StringComparison.Ordinal),
                            $"{Path.GetFileName(file)} depends on {assembly}");
                    }
                }

                string client = File.ReadAllText(Path.Combine(output, "Bank", "Client.cs"));
                StringAssert.Contains(client, "Example.Banking.Runtime.Soap.OnvifClientBase");
                StringAssert.Contains(client, "Example.Banking.Runtime.Xml.OnvifContract");

                string @base = File.ReadAllText(Path.Combine(runtime, "Soap", "OnvifClientBase.cs"));
                StringAssert.Contains(@base, "namespace Example.Banking.Runtime.Soap");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void WritesTheRuntimeWhereItWasPointed()
        {
            string output = NewOutputDirectory();
            string elsewhere = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--runtime-namespace", "Example.Soap", "--runtime-out", elsewhere]);

                new CodeGenerator(options!).Run();

                Assert.IsFalse(Directory.Exists(Path.Combine(output, "Runtime")),
                    "the runtime went where it was pointed, not where it defaults to");

                string @base = File.ReadAllText(Path.Combine(elsewhere, "Soap", "OnvifClientBase.cs"));
                StringAssert.Contains(@base, "namespace Example.Soap.Soap");

                string client = File.ReadAllText(Path.Combine(output, "Bank", "Client.cs"));
                StringAssert.Contains(client, "Example.Soap.Soap.OnvifClientBase");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
                Directory.Delete(elsewhere, recursive: true);
            }
        }

        [TestMethod]
        public void CompilesAgainstARuntimeItWasToldNotToWrite()
        {
            // A second run generating into the same solution must not emit a second copy of the
            // runtime, but still has to name the one that is already there.
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--runtime-namespace", "Example.Shared.Soap", "--no-runtime"]);

                new CodeGenerator(options!).Run();

                Assert.IsFalse(Directory.Exists(Path.Combine(output, "Runtime")));

                string client = File.ReadAllText(Path.Combine(output, "Bank", "Client.cs"));
                StringAssert.Contains(client, "Example.Shared.Soap.Soap.OnvifClientBase");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void RefusesToBothWriteAndNotWriteTheRuntime()
        {
            Assert.ThrowsExactly<SchemaException>(() => CommandLine.Parse(
                ["--wsdl", Fixture, "--namespace", "Example", "--out", "out",
                 "--no-runtime", "--runtime-out", "somewhere"]));
        }

        [TestMethod]
        public void BuildsItsOwnSettingsFromTheTypeItWasGiven()
        {
            // Which defaults are sensible, what a client authenticates with, what it declares on
            // its envelopes - none of that is WSDL's business, so the run names a type that knows
            // and the generator writes none of it.
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--settings", "Example.Banking.BankSettings"]);

                new CodeGenerator(options!).Run();

                string client = File.ReadAllText(Path.Combine(output, "Bank", "Client.cs"));

                StringAssert.Contains(client, "new Example.Banking.BankSettings()");
                StringAssert.Contains(client, "new Example.Banking.BankSettings(userName, password)");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void IsOnlyEverHandedSettingsWhenNoneWereNamed()
        {
            // Nothing to construct means no constructor that constructs it, rather than a client
            // that compiles and then talks to nothing.
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output]);

                new CodeGenerator(options!).Run();

                string client = File.ReadAllText(Path.Combine(output, "Bank", "Client.cs"));

                StringAssert.Contains(client, "AccountsClient(string endpointUri, Example.Banking.Runtime.Soap.IClientSettings settings)");
                Assert.IsFalse(client.Contains("AccountsClient(string endpointUri)", StringComparison.Ordinal),
                    "there is nothing it could have built those settings from");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [DataRow("m", DisplayName = "no namespace")]
        [DataRow("=urn:example:money", DisplayName = "no prefix")]
        [DataRow("m=", DisplayName = "empty namespace")]
        [TestMethod]
        public void RejectsAnEnvelopePrefixItCannotRead(string argument)
        {
            Assert.ThrowsExactly<SchemaException>(() => CommandLine.Parse(
                ["--wsdl", Fixture, "--namespace", "Example", "--out", "out",
                 "--envelope-prefix", argument]));
        }

        [TestMethod]
        public void KeepsTheCommittedRuntimeInStepWithItsSource()
        {
            // The runtime in SharpOnvifCommon is generated, but it is committed and built like any
            // other source, so nothing would notice it drifting from the source it is emitted from
            // - an edit to the emitted copy, or an edit to the template that was never regenerated.
            // This is what notices. Run `dotnet run --project src/WsdlGenerator` to settle it.
            string repository = RepositoryRoot();
            string committed = Path.Combine(repository, "src", "SharpOnvifCommon", "Generated", "Runtime");

            string output = NewOutputDirectory();
            try
            {
                var options = ServiceCatalog.OnvifOptions(repository, Path.Combine(repository, "src"));
                new RuntimeEmitter(options.Runtime.Namespace).Emit(output);

                var emitted = Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(output, f))
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();

                CollectionAssert.AreEqual(
                    emitted,
                    Directory.GetFiles(committed, "*.cs", SearchOption.AllDirectories)
                        .Select(f => Path.GetRelativePath(committed, f))
                        .OrderBy(f => f, StringComparer.Ordinal)
                        .ToList(),
                    "the committed runtime is not the set of files the generator writes");

                foreach (string file in emitted)
                {
                    Assert.AreEqual(
                        File.ReadAllText(Path.Combine(output, file)),
                        File.ReadAllText(Path.Combine(committed, file)),
                        $"{file} differs from what the generator would write");
                }
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        /// <summary>
        /// Walks up to the repository, which is where the schema mirror and the projects live.
        /// </summary>
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "wsdl"))
                    && Directory.Exists(Path.Combine(directory.FullName, "src")))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            throw new AssertFailedException("Could not find the repository root from the test binary.");
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
        public void AddsAValueTheSchemaDoesNotEnumerate()
        {
            // A published schema trails what implementations send. The value is configured for the
            // generator rather than edited into its output, so regenerating keeps it - and keeps
            // the conversions generated beside the enum in step with it.
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--enum-value", "{urn:example:money}Currency=GBP"]);

                new CodeGenerator(options!).Run();

                string shared = File.ReadAllText(Path.Combine(output, "Schema", "DataContracts.cs"));

                StringAssert.Contains(shared, "GBP,", "the enum has to be able to name the value");
                StringAssert.Contains(shared, "case Currency.GBP:", "writing it has to produce its XML form");
                StringAssert.Contains(shared, "return Currency.GBP;", "reading that form back has to produce it");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void LeavesAValueTheSchemaAlreadyEnumeratesAlone()
        {
            // A schema that catches up makes the configured value redundant, not wrong.
            string output = NewOutputDirectory();
            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--enum-value", "{urn:example:money}Currency=EUR"]);

                new CodeGenerator(options!).Run();

                string shared = File.ReadAllText(Path.Combine(output, "Schema", "DataContracts.cs"));

                Assert.AreEqual(1, Occurrences(shared, "case Currency.EUR:"),
                    "the value the schema now carries must not be added a second time");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [TestMethod]
        public void RefusesToAddAValueToSomethingThatIsNotAnEnumeration()
        {
            string output = NewOutputDirectory();
            try
            {
                var notAnEnumeration = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--enum-value", "{urn:example:money}Money=GBP"]);

                Assert.ThrowsExactly<SchemaException>(() => new CodeGenerator(notAnEnumeration!).Run());

                var noSuchType = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output,
                     "--enum-value", "{urn:example:money}Nonexistent=GBP"]);

                Assert.ThrowsExactly<SchemaException>(() => new CodeGenerator(noSuchType!).Run(),
                    "a typo in the type name must not pass silently");
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        [DataRow("Currency=GBP", DisplayName = "no namespace braces")]
        [DataRow("{urn:example:money}Currency", DisplayName = "no value")]
        [DataRow("{urn:example:money}Currency=", DisplayName = "empty value")]
        [DataRow("{urn:example:money=GBP", DisplayName = "unclosed namespace")]
        [DataRow("{urn:example:money}=GBP", DisplayName = "no local name")]
        [TestMethod]
        public void RejectsAnEnumValueItCannotRead(string argument)
        {
            Assert.ThrowsExactly<SchemaException>(() => CommandLine.Parse(
                ["--wsdl", Fixture, "--namespace", "Example", "--out", "out", "--enum-value", argument]));
        }

        private static int Occurrences(string text, string value)
        {
            int count = 0;
            for (int i = text.IndexOf(value, StringComparison.Ordinal); i >= 0;
                 i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
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
