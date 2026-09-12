using System;
using System.Collections.Generic;
using System.Xml;

namespace SharpOnvifCommon.Xml
{
    /// <summary>
    /// Reads Onvif data contracts from XML. Generated types dispatch on the current element name
    /// and call these helpers, so deserialization is compiled rather than reflected.
    /// <para>
    /// Reading is intentionally lenient. Elements are matched by name rather than position, and
    /// anything the schema does not describe is skipped instead of faulting: real devices reorder
    /// sequences, omit optional elements, and add vendor extensions, and a strict reader would
    /// reject responses that are otherwise perfectly usable.
    /// </para>
    /// </summary>
    public sealed class OnvifXmlReader
    {
        private readonly XmlReader _reader;
        private readonly Func<string, string, OnvifContract> _typeFactory;
        private XmlDocument _ownerDocument;
        private readonly IXmlLineInfo _lineInfo;

        /// <param name="reader">Positioned anywhere; the read helpers move it.</param>
        /// <param name="typeFactory">
        /// Resolves an xsi:type name to an instance. Each generated assembly supplies its own,
        /// because each carries its own copy of the shared schema types.
        /// </param>
        public OnvifXmlReader(XmlReader reader, Func<string, string, OnvifContract> typeFactory = null)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _typeFactory = typeFactory;
            _lineInfo = reader as IXmlLineInfo;
        }

        /// <summary>The underlying reader, for content this class does not model.</summary>
        public XmlReader Xml { get { return _reader; } }

        /// <summary>Local name of the element or attribute the reader is positioned on.</summary>
        public string LocalName { get { return _reader.LocalName; } }

        /// <summary>Namespace of the element or attribute the reader is positioned on.</summary>
        public string NamespaceUri { get { return _reader.NamespaceURI; } }

        /// <summary>Value of the attribute the reader is positioned on.</summary>
        public string AttributeValue { get { return _reader.Value; } }

        /// <summary>True when the reader is on an element matching the given name.</summary>
        public bool IsElement(string ns, string name)
        {
            return _reader.LocalName == name && _reader.NamespaceURI == ns;
        }

        // ------------------------------------------------------------------ scalars

        /// <summary>
        /// Reads the character content of the current element and leaves the reader on the node
        /// after it. An empty element yields an empty string, not null, so a present-but-empty
        /// element is distinguishable from an absent one.
        /// </summary>
        public string ReadElementText()
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return string.Empty;
            }

            return _reader.ReadElementContentAsString();
        }

        /// <summary>
        /// Resolves an xs:QName lexical form against the namespace scope the reader is currently
        /// in. Must be called while still positioned inside the element that carried the value.
        /// </summary>
        public XmlQualifiedName ToQualifiedName(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            text = text.Trim();
            int colon = text.IndexOf(':');
            if (colon < 0) return new XmlQualifiedName(text, _reader.LookupNamespace(string.Empty) ?? string.Empty);

            string prefix = text.Substring(0, colon);
            string local = text.Substring(colon + 1);
            return new XmlQualifiedName(local, _reader.LookupNamespace(prefix) ?? string.Empty);
        }

        /// <summary>
        /// Reads an element whose content is an xs:QName. The prefix is resolved before the
        /// reader leaves the element, because the binding goes out of scope with it.
        /// </summary>
        public XmlQualifiedName ReadElementQualifiedName()
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return null;
            }

            // Resolve against the element's own scope: ReadElementContentAsString moves past it.
            string raw = _reader.ReadElementContentAsString();
            return ToQualifiedName(raw);
        }

        // ------------------------------------------------------------------ objects

        /// <summary>
        /// Reads the current element into a data contract. When the element carries xsi:type the
        /// named type is constructed instead of <paramref name="create"/>'s, which is how schema
        /// extensions come back as their real type.
        /// </summary>
        public T ReadElementObject<T>(Func<T> create) where T : OnvifContract
        {
            OnvifContract instance = CreateInstance(create);
            ReadInto(instance);
            return instance as T;
        }

        /// <summary>
        /// Reads the attributes and children of the element the reader is positioned on into an
        /// existing instance, and leaves the reader on the node after that element.
        /// </summary>
        public void ReadInto(OnvifContract instance)
        {
            bool empty = _reader.IsEmptyElement;

            ReadAttributes(instance);

            if (empty)
            {
                _reader.Read();
                return;
            }

            _reader.Read();   // step past the start tag

            while (!_reader.EOF)
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.EndElement:
                        _reader.Read();
                        return;

                    case XmlNodeType.Element:
                        long before = Position();
                        if (instance == null || !instance.InvokeReadXmlElement(this))
                        {
                            _reader.Skip();
                        }
                        else if (Position() == before)
                        {
                            // Generated handlers always consume the element they claim. If one
                            // ever does not, skipping here turns an infinite loop into a lost
                            // element, which is the better failure.
                            _reader.Skip();
                        }
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                        if (instance != null) instance.InvokeReadXmlText(this, _reader.Value);
                        _reader.Read();
                        break;

                    default:
                        _reader.Read();
                        break;
                }
            }
        }

        private OnvifContract CreateInstance<T>(Func<T> create) where T : OnvifContract
        {
            string hint = _reader.GetAttribute("type", OnvifXmlNamespaces.XmlSchemaInstance);
            if (hint != null && _typeFactory != null)
            {
                string prefix = string.Empty;
                string local = hint;
                int colon = hint.IndexOf(':');
                if (colon >= 0)
                {
                    prefix = hint.Substring(0, colon);
                    local = hint.Substring(colon + 1);
                }

                string ns = _reader.LookupNamespace(prefix) ?? string.Empty;
                OnvifContract resolved = _typeFactory(ns, local);

                // Only honour the hint if it names something assignable to the declared type;
                // a device that sends a nonsensical xsi:type should not break the whole response.
                if (resolved is T) return resolved;
            }

            return create();
        }

        private void ReadAttributes(OnvifContract instance)
        {
            if (!_reader.HasAttributes) return;
            if (!_reader.MoveToFirstAttribute()) return;

            do
            {
                // Namespace declarations and schema-instance bookkeeping are not data.
                if (_reader.NamespaceURI == OnvifXmlNamespaces.Xmlns) continue;
                if (_reader.Prefix == "xmlns" || _reader.LocalName == "xmlns") continue;
                if (_reader.NamespaceURI == OnvifXmlNamespaces.XmlSchemaInstance) continue;

                if (instance != null) instance.InvokeReadXmlAttribute(this);
            }
            while (_reader.MoveToNextAttribute());

            _reader.MoveToElement();
        }

        /// <summary>
        /// A monotonic marker for where the reader is, used only to notice that a handler
        /// consumed nothing. Returns -1 when the reader cannot report a position, which
        /// disables the check rather than guessing.
        /// </summary>
        private long Position()
        {
            if (_lineInfo == null || !_lineInfo.HasLineInfo()) return -1;
            return ((long)_lineInfo.LineNumber << 32) | (uint)_lineInfo.LinePosition;
        }

        /// <summary>True when the current element is marked xsi:nil.</summary>
        public bool IsNil()
        {
            string nil = _reader.GetAttribute("nil", OnvifXmlNamespaces.XmlSchemaInstance);
            return nil == "true" || nil == "1";
        }

        /// <summary>
        /// Reads the repeated children of a wrapper element. <paramref name="onItem"/> is called
        /// with the reader positioned on each matching child and must consume it; anything else
        /// inside the wrapper is skipped. The reader ends up on the node after the wrapper.
        /// </summary>
        public void ReadWrappedArray(string itemNamespace, string itemName, Action onItem)
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return;
            }

            int wrapperDepth = _reader.Depth;
            _reader.Read();

            while (!_reader.EOF)
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.Depth == wrapperDepth)
                {
                    _reader.Read();
                    return;
                }

                if (_reader.NodeType == XmlNodeType.Element
                    && _reader.LocalName == itemName
                    && (string.IsNullOrEmpty(itemNamespace) || _reader.NamespaceURI == itemNamespace))
                {
                    long before = Position();
                    onItem();
                    if (Position() == before) _reader.Skip();
                }
                else if (_reader.NodeType == XmlNodeType.Element)
                {
                    _reader.Skip();
                }
                else
                {
                    _reader.Read();
                }
            }
        }

        // ------------------------------------------------------------------ wildcards

        /// <summary>
        /// Captures the current element verbatim, for an xs:any wildcard. All captured nodes in
        /// one read share an owner document, which keeps them usable together.
        /// </summary>
        public XmlElement ReadAnyElement()
        {
            if (_ownerDocument == null) _ownerDocument = new XmlDocument();

            using (XmlReader subtree = _reader.ReadSubtree())
            {
                subtree.Read();
                XmlNode node = _ownerDocument.ReadNode(subtree);
                return node as XmlElement;
            }
        }

        /// <summary>
        /// Reads an element whose content is a single arbitrary element and returns that element.
        /// <para>
        /// A schema wrapper that holds nothing but one xs:any carries no information of its own,
        /// so the generated member holds the payload. wsnt:NotificationMessageHolderType/Message
        /// is the one that matters: the wrapper is fixed by the schema and the payload is the
        /// event.
        /// </para>
        /// </summary>
        public XmlElement ReadWrappedElement()
        {
            if (_reader.IsEmptyElement)
            {
                _reader.Read();
                return null;
            }

            int wrapperDepth = _reader.Depth;
            XmlElement payload = null;
            _reader.Read();

            while (!_reader.EOF)
            {
                if (_reader.NodeType == XmlNodeType.EndElement && _reader.Depth == wrapperDepth)
                {
                    _reader.Read();
                    return payload;
                }

                if (_reader.NodeType == XmlNodeType.Element && payload == null)
                {
                    payload = ReadAnyElement();
                }
                else if (_reader.NodeType == XmlNodeType.Element)
                {
                    _reader.Skip();
                }
                else
                {
                    _reader.Read();
                }
            }

            return payload;
        }

        /// <summary>
        /// Makes a text node belonging to the same document as the elements captured alongside it,
        /// for the character data of a mixed-content type.
        /// </summary>
        public XmlNode CreateTextNode(string text)
        {
            if (_ownerDocument == null) _ownerDocument = new XmlDocument();
            return _ownerDocument.CreateTextNode(text);
        }

        /// <summary>Captures the current node verbatim, including character data in mixed content.</summary>
        public XmlNode ReadAnyNode()
        {
            if (_ownerDocument == null) _ownerDocument = new XmlDocument();

            if (_reader.NodeType == XmlNodeType.Text || _reader.NodeType == XmlNodeType.CDATA)
            {
                XmlNode text = _ownerDocument.CreateTextNode(_reader.Value);
                _reader.Read();
                return text;
            }

            return ReadAnyElement();
        }

        /// <summary>Skips the current node.</summary>
        public void Skip()
        {
            _reader.Skip();
        }
    }

    /// <summary>Array growth helpers used by generated readers.</summary>
    public static class OnvifArray
    {
        /// <summary>
        /// Appends one element to an array member.
        /// <para>
        /// Onvif collections are small (profiles, tokens, presets number in the tens), so growing
        /// by one keeps the generated code simple at a cost that does not matter here.
        /// </para>
        /// </summary>
        public static void Append<T>(ref T[] array, T item)
        {
            if (array == null)
            {
                array = new T[] { item };
                return;
            }

            T[] grown = new T[array.Length + 1];
            Array.Copy(array, grown, array.Length);
            grown[array.Length] = item;
            array = grown;
        }

        /// <summary>Splits an xs:list lexical form on whitespace.</summary>
        public static string[] SplitList(string text)
        {
            if (string.IsNullOrEmpty(text)) return new string[0];
            return text.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Joins an xs:list back into its lexical form.</summary>
        public static string JoinList(IEnumerable<string> values)
        {
            if (values == null) return null;
            return string.Join(" ", values);
        }
    }
}
