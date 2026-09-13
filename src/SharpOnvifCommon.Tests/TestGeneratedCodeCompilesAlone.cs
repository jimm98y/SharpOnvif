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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WsdlGenerator.Configuration;
using WsdlGenerator.Generation;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// That generated code depends on nothing of ours, proved by compiling it against nothing of
    /// ours.
    /// </summary>
    /// <remarks>
    /// The point of writing the runtime rather than referencing it, and the one claim the rest of
    /// the suite cannot check: every other test compiles the emitted runtime inside
    /// SharpOnvifCommon, where the implementations it might accidentally reach for happen to sit.
    /// A runtime that quietly used one would build here and fail for everybody else.
    /// <para>
    /// So the output is compiled on its own, against the framework and nothing else - not the
    /// assemblies this test runs in, which are filtered out by name.
    /// </para>
    /// </remarks>
    [TestClass]
    public sealed class TestGeneratedCodeCompilesAlone
    {
        private static string Fixture => Path.Combine(AppContext.BaseDirectory, "Wsdl", "bank.wsdl");

        [TestMethod]
        public void CompilesWithNothingOfOursReferenced()
        {
            string output = Path.Combine(
                Path.GetTempPath(), "sharponvif-standalone-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);

            try
            {
                var options = CommandLine.Parse(
                    ["--wsdl", Fixture, "--namespace", "Example.Banking", "--out", output, "--client"]);

                new CodeGenerator(options!).Run();

                var trees = Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories)
                    .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f))
                    .ToList();

                Assert.IsTrue(trees.Count > 5, "the run produced almost nothing to compile");

                var compilation = CSharpCompilation.Create(
                    "Standalone",
                    trees,
                    FrameworkReferences(),
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                var errors = compilation.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .ToList();

                Assert.AreEqual(
                    0, errors.Count,
                    "generated code needs something it was not given:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors.Take(10)));
            }
            finally
            {
                Directory.Delete(output, recursive: true);
            }
        }

        /// <summary>
        /// The framework this test runs on, with everything of ours removed - which is what makes
        /// the compilation mean something.
        /// </summary>
        private static IEnumerable<MetadataReference> FrameworkReferences()
        {
            string assemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            Assert.IsNotNull(assemblies, "no framework to compile against");

            var references = new List<MetadataReference>();
            foreach (string path in assemblies.Split(Path.PathSeparator))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                if (name.StartsWith("SharpOnvif", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.StartsWith("WsdlGenerator", StringComparison.OrdinalIgnoreCase)) continue;

                references.Add(MetadataReference.CreateFromFile(path));
            }

            return references;
        }
    }
}
