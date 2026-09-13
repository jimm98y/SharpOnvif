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
using System.Globalization;
using System.Security;
using System.Text;
using System.Xml;

namespace SharpOnvifServer
{
    /// <summary>
    /// Makes a value that arrived in a request fit to be written somewhere it will be read as
    /// something other than plain data.
    /// </summary>
    /// <remarks>
    /// A SOAP action, or a discovery datagram, is whatever the sender chose to put there. Written
    /// into a log unchanged, a newline in it starts what looks like a new log entry, and a caller
    /// can compose entries that never happened. Concatenated into an XML document unchanged, a
    /// '&lt;' in it is markup. Both are fixed the same way: turn what cannot be data back into
    /// data, and cap how much of it there is.
    /// </remarks>
    internal static class UntrustedText
    {
        /// <summary>
        /// How much of an untrusted value is kept. Long enough for any real Onvif action, short
        /// enough that a caller cannot fill a log with one request.
        /// </summary>
        public const int DefaultMaxLength = 256;

        /// <summary>
        /// Renders a value as a single line of printable text, for a log entry or for anywhere
        /// that escapes markup itself - <see cref="XmlWriter.WriteString"/>, say. Control
        /// characters become their escape sequences rather than acting, and anything longer than
        /// <paramref name="maxLength"/> is cut short.
        /// </summary>
        public static string Printable(string value, int maxLength = DefaultMaxLength)
        {
            if (maxLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var text = new StringBuilder(Math.Min(value.Length, maxLength) + 4);
            int index = 0;
            int kept = 0;

            while (index < value.Length && kept < maxLength)
            {
                char character = value[index];

                // A surrogate pair is one character and is left alone; a lone surrogate is not a
                // character at all, and is escaped below with everything else that is not.
                if (char.IsHighSurrogate(character) &&
                    index + 1 < value.Length &&
                    char.IsLowSurrogate(value[index + 1]))
                {
                    text.Append(character).Append(value[index + 1]);
                    index += 2;
                    kept++;
                    continue;
                }

                switch (character)
                {
                    case '\r': text.Append("\\r"); break;
                    case '\n': text.Append("\\n"); break;
                    case '\t': text.Append("\\t"); break;
                    case '\\': text.Append("\\\\"); break;
                    default:
                        if (char.IsControl(character) || !XmlConvert.IsXmlChar(character))
                        {
                            text.Append("\\u")
                                .Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            text.Append(character);
                        }
                        break;
                }

                index++;
                kept++;
            }

            if (index < value.Length)
                text.Append("...");

            return text.ToString();
        }

        /// <summary>
        /// Renders a value as the text of an element in a document being built by concatenation,
        /// where nothing else is going to escape it.
        /// </summary>
        public static string ForXmlText(string value, int maxLength = DefaultMaxLength)
        {
            string printable = Printable(value, maxLength);
            return printable.Length == 0 ? string.Empty : SecurityElement.Escape(printable);
        }
    }
}
