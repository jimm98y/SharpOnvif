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

using SharpOnvifCommon.Onvif;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SharpOnvifServer.Events
{
    /// <summary>
    /// The topics a subscriber asked for, and whether a notification is one of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A client subscribing to events says which it wants, as a topic expression in the Filter of
    /// its Subscribe or CreatePullPointSubscription: a client watching for motion asks for
    /// <c>tns1:RuleEngine/CellMotionDetector/Motion</c> and does not expect to be sent everything
    /// else the device produces. A device that ignores the filter is not conformant, and in
    /// practice floods the client.
    /// </para>
    /// <para>
    /// The dialects Onvif devices are asked for are supported: a concrete topic, a set of them
    /// separated by <c>|</c>, <c>*</c> for one level, and a trailing <c>//.</c> for a topic and
    /// everything beneath it. An expression in a dialect this does not recognise matches
    /// everything, which errs towards sending a subscriber too much rather than silently sending
    /// it nothing.
    /// </para>
    /// </remarks>
    public sealed class TopicFilter
    {
        private const string WsnBaseNotification = "http://docs.oasis-open.org/wsn/b-2";

        /// <summary>Dialects whose expression is a topic path, which is all of the ones Onvif uses.</summary>
        private static readonly string[] TopicPathDialects =
        {
            "http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet",
            "http://docs.oasis-open.org/wsn/t-1/TopicExpression/Concrete",
            "http://docs.oasis-open.org/wsn/t-1/TopicExpression/Simple",
            "http://docs.oasis-open.org/wsn/t-1/TopicExpression/Full",
        };

        private readonly List<Term> _terms;

        private TopicFilter(List<Term> terms)
        {
            _terms = terms;
        }

        /// <summary>A filter that lets everything through, which is what no filter means.</summary>
        public static TopicFilter MatchAll { get; } = new TopicFilter(null);

        /// <summary>True when this filter was not narrowed to anything.</summary>
        public bool IsMatchAll
        {
            get { return _terms == null || _terms.Count == 0; }
        }

        /// <summary>
        /// Reads the topics a subscriber asked for out of the Filter it sent. A null or empty
        /// filter, or one that names no topic expression, asks for everything.
        /// </summary>
        public static TopicFilter FromFilter(FilterType filter)
        {
            if (filter == null || filter.Any == null)
                return MatchAll;

            var terms = new List<Term>();

            foreach (XmlElement element in filter.Any)
            {
                if (element == null) continue;
                if (element.LocalName != "TopicExpression") continue;
                if (element.NamespaceURI != WsnBaseNotification && element.NamespaceURI.Length > 0) continue;

                string dialect = element.GetAttribute("Dialect");
                if (dialect.Length > 0 && Array.IndexOf(TopicPathDialects, dialect) < 0)
                {
                    // A dialect we cannot evaluate. Saying "everything" is the safe answer: a
                    // subscriber that receives too much can still work.
                    return MatchAll;
                }

                foreach (Term term in ParseExpression(element))
                {
                    terms.Add(term);
                }
            }

            return terms.Count == 0 ? MatchAll : new TopicFilter(terms);
        }

        /// <summary>True when a notification on this topic is one the subscriber asked for.</summary>
        public bool Matches(string topicNamespace, string topicPath)
        {
            if (IsMatchAll) return true;
            if (string.IsNullOrEmpty(topicPath)) return false;

            string[] segments = Split(topicPath);

            foreach (Term term in _terms)
            {
                if (term.Matches(topicNamespace, segments)) return true;
            }

            return false;
        }

        /// <summary>True when the message is one the subscriber asked for.</summary>
        public bool Matches(NotificationMessage message)
        {
            if (message == null) return false;
            return Matches(message.TopicNamespace, message.Topic);
        }

        private static IEnumerable<Term> ParseExpression(XmlElement expression)
        {
            string text = expression.InnerText;
            if (string.IsNullOrEmpty(text)) yield break;

            // A ConcreteSet is alternatives separated by '|'; whitespace separates them too, and
            // the expression is routinely written across lines.
            foreach (string alternative in text.Split('|'))
            {
                foreach (string candidate in alternative.Split(new[] { ' ', '\t', '\r', '\n' },
                                                               StringSplitOptions.RemoveEmptyEntries))
                {
                    Term term = ParseTerm(candidate, expression);
                    if (term != null) yield return term;
                }
            }
        }

        private static Term ParseTerm(string candidate, XmlElement scope)
        {
            string path = candidate.Trim();
            if (path.Length == 0) return null;

            // "tns1:RuleEngine//." is that topic and everything beneath it.
            bool anyDescendant = false;
            if (path.EndsWith("//.", StringComparison.Ordinal))
            {
                anyDescendant = true;
                path = path.Substring(0, path.Length - 3);
            }

            path = path.TrimEnd('/');
            if (path.Length == 0) return null;

            // The root of a path is a QName, so its prefix is resolved where it was written.
            string ns = null;
            int colon = path.IndexOf(':');
            if (colon >= 0)
            {
                string prefix = path.Substring(0, colon);
                path = path.Substring(colon + 1);
                ns = scope.GetNamespaceOfPrefix(prefix);
                if (string.IsNullOrEmpty(ns)) ns = null;
            }

            string[] segments = Split(path);
            if (segments.Length == 0) return null;

            return new Term(ns, segments, anyDescendant);
        }

        private static string[] Split(string path)
        {
            return path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private sealed class Term
        {
            private readonly string _namespace;
            private readonly string[] _segments;
            private readonly bool _anyDescendant;

            public Term(string ns, string[] segments, bool anyDescendant)
            {
                _namespace = ns;
                _segments = segments;
                _anyDescendant = anyDescendant;
            }

            public bool Matches(string topicNamespace, string[] topicSegments)
            {
                // A term written without a prefix says nothing about the namespace, so it does not
                // constrain one.
                if (_namespace != null && !string.Equals(_namespace, topicNamespace, StringComparison.Ordinal))
                    return false;

                if (_anyDescendant)
                {
                    if (topicSegments.Length < _segments.Length) return false;
                }
                else if (topicSegments.Length != _segments.Length)
                {
                    return false;
                }

                for (int i = 0; i < _segments.Length; i++)
                {
                    if (_segments[i] == "*") continue;
                    if (!string.Equals(_segments[i], topicSegments[i], StringComparison.Ordinal)) return false;
                }

                return true;
            }
        }
    }
}
