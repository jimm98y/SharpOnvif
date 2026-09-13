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
using SharpOnvifServer;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Turning a value that came from a request back into data, before it is written somewhere it
    /// would otherwise be read as something other than data.
    /// </summary>
    [TestClass]
    public sealed class TestUntrustedText
    {
        [TestMethod]
        public void LeavesAnOrdinaryValueAlone()
        {
            const string Action = "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation";

            Assert.AreEqual(Action, UntrustedText.Printable(Action));
            Assert.AreEqual(string.Empty, UntrustedText.Printable(null));
            Assert.AreEqual(string.Empty, UntrustedText.Printable(""));
        }

        [DataRow("a\rb", @"a\rb", DisplayName = "carriage return")]
        [DataRow("a\nb", @"a\nb", DisplayName = "line feed")]
        [DataRow("a\tb", @"a\tb", DisplayName = "tab")]
        [DataRow(@"a\b", @"a\\b", DisplayName = "backslash, so an escape cannot be forged either")]
        [DataRow("a\u0000b", @"a\u0000b", DisplayName = "null")]
        [DataRow("a\u0008b", @"a\u0008b", DisplayName = "backspace")]
        [DataRow("a\u001Bb", @"a\u001Bb", DisplayName = "escape, which a terminal would act on")]
        [DataRow("a\u0085b", @"a\u0085b", DisplayName = "next line")]
        [TestMethod]
        public void TurnsWhatWouldActIntoWhatReads(string value, string expected)
        {
            Assert.AreEqual(expected, UntrustedText.Printable(value));
        }

        [TestMethod]
        public void KeepsAForgedLogEntryOnOneLine()
        {
            // The shape of the attack: a value carrying what looks like the end of this entry and
            // the beginning of another.
            string forged =
                "Probe\r\nfail: Something[0]\r\n      Administrator logged in";

            string printable = UntrustedText.Printable(forged);

            Assert.IsFalse(printable.Contains("\r"), "a carriage return survived");
            Assert.IsFalse(printable.Contains("\n"), "a line feed survived");
            StringAssert.Contains(printable, "Administrator logged in",
                "the value still has to be readable - the point is to disarm it, not to hide it");
        }

        [TestMethod]
        public void WillNotLetOneValueFillTheLog()
        {
            string flood = new string('x', 100000);

            string printable = UntrustedText.Printable(flood);

            Assert.AreEqual(UntrustedText.DefaultMaxLength + 3, printable.Length);
            StringAssert.EndsWith(printable, "...");

            Assert.AreEqual("abc", UntrustedText.Printable("abc", 3), "nothing is cut that fits");
            Assert.AreEqual("ab...", UntrustedText.Printable("abc", 2));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => UntrustedText.Printable("abc", 0));
        }

        [TestMethod]
        public void CountsACharacterThatTakesTwoSlotsAsOne()
        {
            // A surrogate pair is one character and has to survive as one; a lone surrogate is not
            // a character at all, and is escaped like anything else that is not.
            Assert.AreEqual("\U0001F600\U0001F600", UntrustedText.Printable("\U0001F600\U0001F600", 2));
            Assert.AreEqual(@"\uD83D", UntrustedText.Printable("\uD83D"), "a lone high surrogate");
            Assert.AreEqual(@"\uDE00", UntrustedText.Printable("\uDE00"), "a lone low surrogate");
        }

        [TestMethod]
        public void WillNotLetAValueBecomeMarkup()
        {
            // For a document built by concatenation, where nothing else is going to escape it.
            Assert.AreEqual(
                "urn:uuid:1 &lt;evil/&gt; &amp; more",
                UntrustedText.ForXmlText("urn:uuid:1 <evil/> & more"));

            Assert.AreEqual(string.Empty, UntrustedText.ForXmlText(null));
        }
    }
}
