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

using SharpOnvifClient.Events;
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
