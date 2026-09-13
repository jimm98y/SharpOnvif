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

namespace SharpOnvifCommon.Security
{
    /// <summary>
    /// Questions worth asking of <see cref="OnvifAuthenticationSettings"/>, which is a description
    /// of what two sides agreed and answers none of them itself.
    /// </summary>
    public static class OnvifAuthenticationExtensions
    {
        /// <summary>True when the given SOAP action may be sent without credentials.</summary>
        /// <remarks>
        /// Onvif calls these PRE_AUTH: a handful of operations a device answers to anyone, so a
        /// client can find out what it is talking to before it knows how to talk to it.
        /// </remarks>
        public static bool IsPreAuth(this OnvifAuthenticationSettings settings, string action)
        {
            return settings != null
                && settings.PreAuthActions != null
                && action != null
                && settings.PreAuthActions.Contains(action);
        }

        /// <summary>True when the settings ask for the given scheme.</summary>
        public static bool Offers(this OnvifAuthenticationSettings settings, DigestAuthentication scheme)
        {
            return settings != null && (settings.Authentication & scheme) != 0;
        }
    }
}
