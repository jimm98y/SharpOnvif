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
