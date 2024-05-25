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
    }
}
