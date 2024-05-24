using OnvifEvents;
using System.Linq;

namespace SharpOnvifClient
{
    public static class OnvifEvents
    {
        private static bool ContainsTrueValue(string eventXml)
        {
            string str = eventXml.ToLowerInvariant();
            return str.Contains("value=\"true\"") || str.Contains("value=\"1\"");
        }

        public static bool? IsMotionDetected(NotificationMessageHolderType message)
        {
            return IsMotionDetected(string.Concat(message.Topic.Any.First().Value, message.Message.InnerXml));
        }
        public static bool? IsMotionDetected(string eventXml)
        {
            if (!IsMotionEvent(eventXml)) return null;
            return ContainsTrueValue(eventXml);
        }
        private static bool IsMotionEvent(string eventXml)
        {
            if (string.IsNullOrEmpty(eventXml))
                return false;

            string str = eventXml.ToLowerInvariant();
            return
                str.Contains("RuleEngine/CellMotionDetector/Motion".ToLowerInvariant()) ||
                str.Contains("RuleEngine/MotionRegionDetector/Motion".ToLowerInvariant()) ||
                str.Contains("VideoSource/MotionAlarm".ToLowerInvariant());
        }

        public static bool? IsTamperDetected(NotificationMessageHolderType message)
        {
            return IsTamperDetected(string.Concat(message.Topic.Any.First().Value, message.Message.InnerXml));
        }
        public static bool? IsTamperDetected(string eventXml)
        {
            if (!IsTamperEvent(eventXml)) return null;
            return ContainsTrueValue(eventXml);
        }
        private static bool IsTamperEvent(string eventXml)
        {
            if (string.IsNullOrEmpty(eventXml))
                return false;

            string str = eventXml.ToLowerInvariant();
            return str.Contains("RuleEngine/TamperDetector/Tamper".ToLowerInvariant());
        }

        public static bool? IsSoundDetected(NotificationMessageHolderType message)
        {
            return IsSoundDetected(string.Concat(message.Topic.Any.First().Value, message.Message.InnerXml));
        }
        public static bool? IsSoundDetected(string eventXml)
        {
            if (!IsSoundEvent(eventXml)) return null;
            return ContainsTrueValue(eventXml);
        }
        private static bool IsSoundEvent(string eventXml)
        {
            if (string.IsNullOrEmpty(eventXml))
                return false;

            string str = eventXml.ToLowerInvariant();
            return str.Contains("AudioAnalytics/Audio/DetectedSound".ToLowerInvariant());
        }
    }
}
