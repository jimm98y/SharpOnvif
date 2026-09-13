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
using System.Xml;

namespace SharpOnvifCommon
{
    public static class OnvifHelpers
    {
        /// <summary>
        /// An Onvif timeout as the xs:duration a device expects.
        /// </summary>
        public static string GetTimeoutInSeconds(int timeoutInSeconds)
        {
            return "PT" + timeoutInSeconds.ToString(CultureInfo.InvariantCulture) + "S";
        }

        /// <summary>
        /// An Onvif timeout as the xs:duration a device expects.
        /// </summary>
        public static string GetTimeoutInMinutes(int timeoutInMinutes)
        {
            return "PT" + timeoutInMinutes.ToString(CultureInfo.InvariantCulture) + "M";
        }

        /// <summary>
        /// Writes a time the way Onvif carries one: UTC, to the millisecond.
        /// </summary>
        /// <remarks>
        /// The value is converted to UTC rather than merely labelled with Z, and formatted with
        /// the invariant culture - a device reading a time stamped in a non-Gregorian calendar
        /// would otherwise be told the wrong year.
        /// </remarks>
        public static string DateTimeToString(DateTime dateTime)
        {
            return ToUtc(dateTime).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Reads a time a device sent, as UTC.
        /// </summary>
        /// <remarks>
        /// xs:dateTime first, which is what the specification calls for and what fixes the offset
        /// of a value that carries one. Devices do send times that are not quite that, so a
        /// looser parse follows - invariant, because a device's clock is not written in the
        /// server's culture.
        /// </remarks>
        public static DateTime StringToDateTime(string dateTime)
        {
            if (string.IsNullOrEmpty(dateTime))
                throw new ArgumentNullException(nameof(dateTime));

            try
            {
                return XmlConvert.ToDateTime(dateTime.Trim(), XmlDateTimeSerializationMode.Utc);
            }
            catch (FormatException)
            {
                return DateTime.Parse(
                    dateTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            }
        }

        /// <summary>
        /// Reads the two forms Onvif and WS-BaseNotification use for a termination time: an
        /// absolute xs:dateTime, or an xs:duration from now.
        /// </summary>
        /// <returns>The moment it names, in UTC.</returns>
        public static DateTime FromAbsoluteOrRelativeDateTimeUTC(DateTime now, string value, DateTime defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return IsDuration(value)
                ? ToUtc(now).Add(FromTimeout(value))
                : StringToDateTime(value);
        }

        /// <summary>
        /// Reads an xs:duration - the form every Onvif timeout takes.
        /// </summary>
        /// <remarks>
        /// The whole grammar, not just the PT&lt;n&gt;S and PT&lt;n&gt;M that Onvif itself tends
        /// to send: a conformant client is entitled to ask for PT1M30S or PT1H, and did not
        /// deserve to be refused.
        /// </remarks>
        /// <exception cref="NotSupportedException">The value is not an xs:duration.</exception>
        public static TimeSpan FromTimeout(string timeout)
        {
            if (string.IsNullOrEmpty(timeout))
                return TimeSpan.Zero;

            try
            {
                return XmlConvert.ToTimeSpan(timeout.Trim());
            }
            catch (FormatException ex)
            {
                throw new NotSupportedException("'" + timeout + "' is not a duration.", ex);
            }
            catch (OverflowException ex)
            {
                throw new NotSupportedException("'" + timeout + "' is too large to be a duration.", ex);
            }
        }

        /// <summary>
        /// True when the value is an xs:duration rather than an xs:dateTime. A duration is the
        /// only one of the two that begins with P, negative ones with -P.
        /// </summary>
        private static bool IsDuration(string value)
        {
            string trimmed = value.TrimStart();
            if (trimmed.Length == 0) return false;

            if (trimmed[0] == '-') trimmed = trimmed.Substring(1);
            return trimmed.Length > 0 && (trimmed[0] == 'P' || trimmed[0] == 'p');
        }

        private static DateTime ToUtc(DateTime value)
        {
            switch (value.Kind)
            {
                case DateTimeKind.Utc:
                    return value;
                case DateTimeKind.Local:
                    return value.ToUniversalTime();
                default:
                    // A time with no zone is taken to be the UTC it is about to be labelled as,
                    // which is what every caller in this library means by one.
                    return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
        }

        public static Uri ChangeUriPath(Uri serviceBaseUri, string path)
        {
            UriBuilder builder = new UriBuilder(serviceBaseUri);
            builder.Path = path;
            return builder.Uri;
        }
    }
}
