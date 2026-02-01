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

namespace SharpOnvifCommon
{
    public static class OnvifHelpers
    {
        public static string GetTimeoutInSeconds(int timeoutInSeconds)
        {
            return $"PT{timeoutInSeconds}S";
        }

        public static string GetTimeoutInMinutes(int timeoutInMinutes)
        {
            return $"PT{timeoutInMinutes}M";
        }

        public static string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public static DateTime StringToDateTime(string dateTime)
        {
            return DateTime.Parse(dateTime);
        }

        public static DateTime FromAbsoluteOrRelativeDateTimeUTC(DateTime now, string value, DateTime defaultValue)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if(value.ToUpperInvariant().StartsWith("PT"))
            {
                return now.Add(FromTimeout(value));
            }
            else
            {
                return StringToDateTime(value);
            }
        }

        public static TimeSpan FromTimeout(string timeout)
        {
            if(string.IsNullOrEmpty(timeout))
                return TimeSpan.Zero;

            timeout = timeout.ToUpperInvariant();
            if(timeout.StartsWith("PT"))
            {
                int number;
                if (int.TryParse(timeout.Substring(2, timeout.Length - 3), out number))
                {
                    if (timeout.EndsWith("M"))
                    {
                        return TimeSpan.FromMinutes(number);
                    }
                    else if (timeout.EndsWith("S"))
                    {
                        return TimeSpan.FromMinutes(number);
                    }
                }
            }

            throw new NotSupportedException(timeout);
        }

        public static Uri ChangeUriPath(Uri serviceBaseUri, string path)
        {
            UriBuilder builder = new UriBuilder(serviceBaseUri);
            builder.Path = path;
            return builder.Uri;
        }
    }
}
