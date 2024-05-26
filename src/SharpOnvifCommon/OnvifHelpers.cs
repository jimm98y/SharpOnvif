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
    }
}
