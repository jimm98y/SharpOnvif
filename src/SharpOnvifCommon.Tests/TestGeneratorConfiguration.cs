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
using WsdlGenerator.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The file a run is described in, and the rule that the generator knows nothing about the
    /// services it generates.
    /// </summary>
    [TestClass]
    public sealed class TestGeneratorConfiguration
    {
        private static string Repository => TestCodeGenerator.RepositoryRoot();

        private static string OnvifConfiguration => Path.Combine(Repository, "onvif.codegen.json");

        [TestMethod]
        public void ReadsThisRepositorysOwnRun()
        {
            var options = ConfigurationFile.Load(OnvifConfiguration);

            Assert.AreEqual(25, options.Services.Count, "every Onvif service SharpOnvif ships");
            Assert.AreEqual("SharpOnvifCommon.Onvif", options.SharedNamespace);
            Assert.AreEqual("SharpOnvifCommon", options.Runtime.Namespace);
            Assert.AreEqual("SharpOnvifCommon.Soap.OnvifClientSettings", options.SettingsType);
            Assert.AreEqual("SharpOnvifServer.Dispatch", options.DispatchNamespace);
            Assert.AreEqual("Onvif", options.TypeNamePrefix);
            Assert.AreEqual(2, options.Targets.Count);
            Assert.AreEqual(5, options.EnumerationExtensions.Count, "the codecs onvif.xsd omits");

            // Relative in the file, absolute once read, so a run does not depend on where it was
            // started from.
            Assert.IsTrue(Path.IsPathRooted(options.SharedDirectory));
            Assert.IsTrue(Directory.Exists(options.MirrorRoot), "the mirror it names has to be there");
        }

        [TestMethod]
        public void SaysWhichFileAndWhatIsWrongWithIt()
        {
            string path = Path.Combine(Path.GetTempPath(), "sharponvif-bad-" + Guid.NewGuid().ToString("N") + ".json");

            try
            {
                File.WriteAllText(path, """
                    {
                      "shared":  { "namespace": "Example.Schema",  "out": "Schema" },
                      "runtime": { "namespace": "Example.Runtime", "out": "Runtime" },
                      "targets": [ { "namespace": "Example", "out": ".", "client": true } ],
                      "services": [ { "name": "Bank" } ]
                    }
                    """);

                var error = Assert.ThrowsExactly<SchemaException>(() => ConfigurationFile.Load(path));

                StringAssert.Contains(error.Message, path, "which file");
                StringAssert.Contains(error.Message, "wsdl", "and what it is missing");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void RefusesAKeyItDoesNotKnow()
        {
            // The quiet one. Misspell typeNamePrefix and every type whose schema name collides
            // with a framework one loses its prefix - which compiles here and collides in every
            // caller's file, from a run that reported success.
            string path = Path.Combine(Path.GetTempPath(), "sharponvif-typo-" + Guid.NewGuid().ToString("N") + ".json");

            try
            {
                File.WriteAllText(path, """
                    {
                      "typeNamePrefx": "Onvif",
                      "shared":  { "namespace": "Example.Schema",  "out": "Schema" },
                      "runtime": { "namespace": "Example.Runtime", "out": "Runtime" },
                      "targets": [ { "namespace": "Example", "out": ".", "client": true } ],
                      "services": [ { "name": "Bank", "wsdl": "bank.wsdl" } ]
                    }
                    """);

                var error = Assert.ThrowsExactly<SchemaException>(() => ConfigurationFile.Load(path));

                StringAssert.Contains(error.Message, "typeNamePrefx", "say which key");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void StillTakesANoteAtTheTopOfTheFile()
        {
            // "$comment" is how a note is written in JSON, and refusing unknown keys must not
            // refuse that - this repository's own file opens with one.
            Assert.IsNotNull(ConfigurationFile.Load(OnvifConfiguration));
        }

        [TestMethod]
        public void RefusesAFileThatIsNotThere()
        {
            var error = Assert.ThrowsExactly<SchemaException>(
                () => ConfigurationFile.Load("nowhere-in-particular.json"));

            StringAssert.Contains(error.Message, "nowhere-in-particular.json");
        }

        [TestMethod]
        public void TheGeneratorNamesNoServiceItGenerates()
        {
            // The point of the configuration file. The generator compiles WSDL; which WSDL is the
            // run's business, and a mention creeping back into the compiler is how that stops
            // being true.
            //
            // Two exceptions, both honest: the copyright header on the runtime source, and the
            // name of this repository's own configuration file, which the generator does look for
            // when it is given no arguments.
            string generator = Path.Combine(Repository, "src", "WsdlGenerator");

            var offenders = Directory.GetFiles(generator, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (File: f, Number: i + 1, Line: line)))
                .Where(l => l.Line.Contains("onvif", StringComparison.OrdinalIgnoreCase))
                .Where(l => !l.Line.Contains("onvif.codegen.json", StringComparison.OrdinalIgnoreCase))
                .Where(l => l.Line.Trim() != "// SharpOnvif")
                .Select(l => $"{Path.GetFileName(l.File)}:{l.Number}: {l.Line.Trim()}")
                .ToList();

            Assert.AreEqual(0, offenders.Count,
                "the generator has learned about a service family:" + Environment.NewLine +
                string.Join(Environment.NewLine, offenders));
        }
    }
}
