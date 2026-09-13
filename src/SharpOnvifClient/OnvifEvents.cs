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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using SharpOnvifCommon.Onvif;

namespace SharpOnvifClient
{
    /// <summary>
    /// Reads the common Onvif notifications: whether this one says motion started, and so on.
    /// </summary>
    /// <remarks>
    /// An Onvif event says what happened in a named data item, not merely somewhere in the
    /// message. A motion notification carries its source alongside - which video source, which
    /// rule - and those items have values of their own. Answering "is there motion" by looking for
    /// any true value in the message reports motion whenever any of them happens to be true, which
    /// for a camera reporting IsMotion=false beside an enabled rule is a false alarm.
    /// </remarks>
    public static class OnvifEvents
    {
        // Onvif names the item that carries the state per topic.
        private static readonly string[] MotionTopics =
        {
            "RuleEngine/CellMotionDetector/Motion",
            "RuleEngine/MotionRegionDetector/Motion",
            "VideoSource/MotionAlarm",
        };

        private static readonly string[] MotionItems = { "IsMotion", "State" };

        private static readonly string[] TamperTopics = { "RuleEngine/TamperDetector/Tamper" };
        private static readonly string[] TamperItems = { "IsTamper", "State" };

        private static readonly string[] SoundTopics = { "AudioAnalytics/Audio/DetectedSound" };
        private static readonly string[] SoundItems = { "IsSoundDetected", "State" };

        /// <summary>Whether this notification reports motion, or null when it is not about motion.</summary>
        public static bool? IsMotionDetected(NotificationMessageHolderType message)
        {
            return Read(message, MotionTopics, MotionItems);
        }

        /// <summary>
        /// The same for a notification still in its XML form, as the Basic subscription listener
        /// hands it over.
        /// </summary>
        public static bool? IsMotionDetected(string eventXml)
        {
            return Read(eventXml, MotionTopics, MotionItems);
        }

        /// <summary>Whether this notification reports tampering, or null when it is not about tampering.</summary>
        public static bool? IsTamperDetected(NotificationMessageHolderType message)
        {
            return Read(message, TamperTopics, TamperItems);
        }

        /// <inheritdoc cref="IsTamperDetected(NotificationMessageHolderType)"/>
        public static bool? IsTamperDetected(string eventXml)
        {
            return Read(eventXml, TamperTopics, TamperItems);
        }

        /// <summary>Whether this notification reports a sound, or null when it is not about sound.</summary>
        public static bool? IsSoundDetected(NotificationMessageHolderType message)
        {
            return Read(message, SoundTopics, SoundItems);
        }

        /// <inheritdoc cref="IsSoundDetected(NotificationMessageHolderType)"/>
        public static bool? IsSoundDetected(string eventXml)
        {
            return Read(eventXml, SoundTopics, SoundItems);
        }

        /// <summary>The topic a notification names, or null when it names none.</summary>
        /// <remarks>
        /// The prefix is left in place and ignored when matching: a device may bind the Onvif
        /// topic namespace to whatever prefix it likes, and tns1 is only a convention.
        /// </remarks>
        public static string GetTopic(NotificationMessageHolderType message)
        {
            XmlNode[] any = message?.Topic?.Any;
            if (any == null) return null;

            foreach (XmlNode node in any)
            {
                if (node == null) continue;

                string value = node.Value ?? node.InnerText;
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }

            return null;
        }

        private static bool? Read(NotificationMessageHolderType message, string[] topics, string[] itemNames)
        {
            if (!IsAbout(GetTopic(message), topics)) return null;
            if (message.Message == null) return null;

            foreach (XmlElement item in SimpleItems(message.Message))
            {
                if (Array.IndexOf(itemNames, item.GetAttribute("Name")) < 0) continue;

                bool? value = ToBoolean(item.GetAttribute("Value"));
                if (value.HasValue) return value;
            }

            return null;
        }

        private static bool? Read(string eventXml, string[] topics, string[] itemNames)
        {
            if (string.IsNullOrEmpty(eventXml)) return null;
            if (!IsAbout(eventXml, topics)) return null;

            // The text may be a whole envelope, or a topic and a message run together, so it is
            // scanned rather than parsed. The item that carries the state is still the one to read.
            foreach (Match tag in SimpleItemTag.Matches(eventXml))
            {
                string name = null;
                string value = null;

                foreach (Match attribute in AttributeInTag.Matches(tag.Value))
                {
                    string attributeName = attribute.Groups["name"].Value;
                    if (string.Equals(attributeName, "Name", StringComparison.OrdinalIgnoreCase))
                        name = attribute.Groups["value"].Value;
                    else if (string.Equals(attributeName, "Value", StringComparison.OrdinalIgnoreCase))
                        value = attribute.Groups["value"].Value;
                }

                if (name == null || Array.IndexOf(itemNames, name) < 0) continue;

                bool? state = ToBoolean(value);
                if (state.HasValue) return state;
            }

            return null;
        }

        /// <summary>Every tt:SimpleItem in the Data of a message, whatever prefix it uses.</summary>
        private static System.Collections.Generic.IEnumerable<XmlElement> SimpleItems(XmlElement message)
        {
            foreach (XmlNode list in message.ChildNodes)
            {
                // The state is in the Data; the Source says which source it came from, and its
                // items are not the answer to anything asked here.
                if (list.LocalName != "Data") continue;

                foreach (XmlNode item in list.ChildNodes)
                {
                    if (item.LocalName == "SimpleItem" && item is XmlElement element) yield return element;
                }
            }
        }

        private static bool IsAbout(string text, string[] topics)
        {
            if (string.IsNullOrEmpty(text)) return false;

            foreach (string topic in topics)
            {
                if (text.IndexOf(topic, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        /// <summary>Reads an xs:boolean, which Onvif writes as true/false or 1/0.</summary>
        private static bool? ToBoolean(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            switch (value.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                    return true;
                case "false":
                case "0":
                    return false;
                default:
                    return null;
            }
        }

        private static readonly Regex SimpleItemTag = new Regex(
            @"<[^<>]*\bSimpleItem\b[^<>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex AttributeInTag = new Regex(
            @"(?<name>[\w.\-]+)\s*=\s*""(?<value>[^""]*)""", RegexOptions.CultureInvariant);
    }
}
