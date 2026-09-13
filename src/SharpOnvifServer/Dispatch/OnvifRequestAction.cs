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

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// The action a request names in its Content-Type.
    /// </summary>
    /// <remarks>
    /// Read in one place because two things decide from it: which operation runs, and - for the
    /// operations Onvif puts in its PRE_AUTH class - whether the device asks for a password at
    /// all. Two readings of one header is how a request comes to authenticate as one operation
    /// and execute as another.
    /// </remarks>
    internal static class OnvifRequestAction
    {
        private const string Parameter = "action=";

        /// <summary>
        /// The action, or null when the header names none - or names more than one, which there
        /// is no honest way to choose between.
        /// </summary>
        public static string FromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return null;

            string action = null;

            // By parameter rather than by substring, so that a parameter merely ending in
            // "action=" is not mistaken for this one.
            foreach (string parameter in contentType.Split(';'))
            {
                string trimmed = parameter.Trim();
                if (!trimmed.StartsWith(Parameter, StringComparison.OrdinalIgnoreCase)) continue;

                // A second one and the caller has told us two different things. Refusing to guess
                // is the whole point: whichever of the two a reader picked, the other reader might
                // pick the other.
                if (action != null) return null;

                // Devices and tools quote this inconsistently, with single quotes, double quotes
                // or none at all.
                action = trimmed.Substring(Parameter.Length).Trim().Trim('"', '\'');
            }

            return string.IsNullOrEmpty(action) ? null : action;
        }
    }
}
