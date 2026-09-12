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

namespace SharpOnvifCommon.Xml
{
    /// <summary>
    /// Base class of every generated Onvif data contract.
    /// <para>
    /// Serialization is compiled into the generated types themselves rather than discovered by
    /// reflection, and these members are the hooks the reader and writer drive. A type that
    /// extends another in the schema overrides them and chains to <c>base</c>, which reproduces
    /// the schema's content model: a complexContent extension writes the base type's elements
    /// before its own.
    /// </para>
    /// <para>
    /// The members are deliberately named so they cannot collide with a schema-derived property.
    /// The generator asserts that no Onvif element or attribute is called any of them.
    /// </para>
    /// </summary>
    public abstract class OnvifObject
    {
        /// <summary>
        /// Writes this type's XML attributes. Overrides call <c>base</c> first so inherited
        /// attributes are written before the derived type's.
        /// </summary>
        protected internal virtual void WriteXmlAttributes(OnvifXmlWriter writer)
        {
        }

        /// <summary>
        /// Writes this type's child elements and character content, in schema sequence order.
        /// Overrides call <c>base</c> first.
        /// </summary>
        protected internal virtual void WriteXmlContent(OnvifXmlWriter writer)
        {
        }

        /// <summary>
        /// Consumes the attribute the reader is positioned on if this type declares it.
        /// Returns false to let the caller ignore an attribute the schema does not describe,
        /// which real devices do send.
        /// </summary>
        protected internal virtual bool ReadXmlAttribute(OnvifXmlReader reader)
        {
            return false;
        }

        /// <summary>
        /// Consumes the element the reader is positioned on if this type declares it. Returns
        /// false to let the caller skip an element the schema does not describe.
        /// <para>
        /// Elements are matched by name rather than by position, so a device that reorders a
        /// sequence or omits an optional element still deserializes.
        /// </para>
        /// </summary>
        protected internal virtual bool ReadXmlElement(OnvifXmlReader reader)
        {
            return false;
        }

        /// <summary>
        /// Receives character content, for simple-content and mixed-content types. The reader is
        /// still positioned inside the element, so a value whose type is an xs:QName can resolve
        /// its prefix against the namespace scope that is about to close.
        /// </summary>
        protected internal virtual void ReadXmlText(OnvifXmlReader reader, string text)
        {
        }

        /// <summary>
        /// Local name of this type in the schema, or null for an anonymous type. Used to decide
        /// whether a value needs an xsi:type hint, and to reconstruct it on the way back in.
        /// </summary>
        protected internal virtual string OnvifXmlTypeName
        {
            get { return null; }
        }

        /// <summary>Namespace of <see cref="OnvifXmlTypeName"/>.</summary>
        protected internal virtual string OnvifXmlTypeNamespace
        {
            get { return null; }
        }
    }
}
