using System;
using System.Xml;

namespace SharpOnvifCommon.Xml
{
    /// <summary>
    /// Writes Onvif data contracts as XML. Generated types call these helpers directly, so there
    /// is no reflection and no serializer to build at startup.
    /// <para>
    /// Null values and null arrays are skipped rather than written as empty elements, which is
    /// what an absent optional element means in the schema.
    /// </para>
    /// </summary>
    public sealed class OnvifXmlWriter
    {
        private readonly XmlWriter _writer;

        public OnvifXmlWriter(XmlWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        /// <summary>The underlying writer, for content this class does not model.</summary>
        public XmlWriter Xml { get { return _writer; } }

        // ------------------------------------------------------------------ elements

        public void WriteStartElement(string ns, string name)
        {
            if (string.IsNullOrEmpty(ns)) _writer.WriteStartElement(name);
            else _writer.WriteStartElement(name, ns);
        }

        public void WriteEndElement()
        {
            _writer.WriteEndElement();
        }

        public void WriteElementString(string ns, string name, string value)
        {
            if (value == null) return;
            WriteStartElement(ns, name);
            _writer.WriteString(value);
            _writer.WriteEndElement();
        }

        /// <summary>Writes an element that carries no value, for an empty complex type.</summary>
        public void WriteEmptyElement(string ns, string name)
        {
            WriteStartElement(ns, name);
            _writer.WriteEndElement();
        }

        /// <summary>
        /// Writes a nested data contract, emitting xsi:type when the runtime type is a schema
        /// extension of the declared one so the reader can reconstruct it.
        /// </summary>
        public void WriteElement(string ns, string name, OnvifContract value, string declaredTypeNamespace, string declaredTypeName)
        {
            if (value == null) return;

            WriteStartElement(ns, name);
            WriteTypeHint(value, declaredTypeNamespace, declaredTypeName);
            value.InvokeWriteXmlAttributes(this);
            value.InvokeWriteXmlContent(this);
            _writer.WriteEndElement();
        }

        /// <summary>Writes the body of a contract into an element the caller has already started.</summary>
        public void WriteContent(OnvifContract value)
        {
            if (value == null) return;
            value.InvokeWriteXmlAttributes(this);
            value.InvokeWriteXmlContent(this);
        }

        private void WriteTypeHint(OnvifContract value, string declaredTypeNamespace, string declaredTypeName)
        {
            string actualName = value.InvokeOnvifXmlTypeName;
            if (actualName == null || declaredTypeName == null) return;
            if (actualName == declaredTypeName && value.InvokeOnvifXmlTypeNamespace == declaredTypeNamespace) return;

            // The prefix has to be declared on this element: the reader resolves xsi:type as a
            // QName against the element's own namespace scope.
            string prefix = _writer.LookupPrefix(value.InvokeOnvifXmlTypeNamespace);
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = "t";
                _writer.WriteAttributeString("xmlns", prefix, OnvifXmlNamespaces.Xmlns, value.InvokeOnvifXmlTypeNamespace);
            }

            _writer.WriteAttributeString("type", OnvifXmlNamespaces.XmlSchemaInstance, prefix + ":" + actualName);
        }

        /// <summary>Writes a nil element, for a nillable member whose value is absent.</summary>
        public void WriteNil(string ns, string name)
        {
            WriteStartElement(ns, name);
            _writer.WriteAttributeString("nil", OnvifXmlNamespaces.XmlSchemaInstance, "true");
            _writer.WriteEndElement();
        }

        /// <summary>
        /// Writes an xs:QName value, whose lexical form is a prefix bound in the element's own
        /// namespace scope. A prefix is declared on the spot when none is in scope.
        /// </summary>
        public void WriteElementQualifiedName(string ns, string name, XmlQualifiedName value)
        {
            if (value == null) return;

            WriteStartElement(ns, name);
            _writer.WriteString(QualifiedNameToString(value));
            _writer.WriteEndElement();
        }

        /// <summary>Renders an xs:QName, declaring a prefix for its namespace if necessary.</summary>
        public string QualifiedNameToString(XmlQualifiedName value)
        {
            if (value == null) return null;
            if (string.IsNullOrEmpty(value.Namespace)) return value.Name;

            string prefix = _writer.LookupPrefix(value.Namespace);
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = "q";
                _writer.WriteAttributeString("xmlns", prefix, OnvifXmlNamespaces.Xmlns, value.Namespace);
            }

            return prefix + ":" + value.Name;
        }

        // ------------------------------------------------------------------ attributes

        public void WriteAttributeString(string ns, string name, string value)
        {
            if (value == null) return;
            if (string.IsNullOrEmpty(ns)) _writer.WriteAttributeString(name, value);
            else _writer.WriteAttributeString(name, ns, value);
        }

        // ------------------------------------------------------------------ text

        public void WriteText(string value)
        {
            if (value == null) return;
            _writer.WriteString(value);
        }

        // ------------------------------------------------------------------ wildcards

        /// <summary>
        /// Writes a single captured element verbatim, tag and all. Used for a schema element
        /// whose whole content is a wildcard, which is carried as the element itself rather than
        /// as a generated wrapper class.
        /// </summary>
        public void WriteAnyElement(XmlNode node)
        {
            if (node == null) return;

            if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
            {
                _writer.WriteString(node.Value);
                return;
            }

            node.WriteTo(_writer);
        }

        /// <summary>Writes the elements captured by an xs:any wildcard back out verbatim.</summary>
        public void WriteAny(XmlElement[] elements)
        {
            if (elements == null) return;
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null) elements[i].WriteTo(_writer);
            }
        }

        /// <summary>
        /// Writes the nodes captured by an xs:any wildcard in a mixed-content type, which may
        /// include the character data between them.
        /// </summary>
        public void WriteAny(XmlNode[] nodes)
        {
            if (nodes == null) return;
            for (int i = 0; i < nodes.Length; i++)
            {
                XmlNode node = nodes[i];
                if (node == null) continue;

                if (node.NodeType == XmlNodeType.Text || node.NodeType == XmlNodeType.CDATA)
                {
                    _writer.WriteString(node.Value);
                }
                else
                {
                    node.WriteTo(_writer);
                }
            }
        }
    }
}
