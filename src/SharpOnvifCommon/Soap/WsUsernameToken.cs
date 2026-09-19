using System;
using System.Globalization;
using System.Xml;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Xml;

namespace SharpOnvifCommon.Soap
{
    /// <summary>
    /// Reads and writes the WS-Security UsernameToken header Onvif uses for its SOAP-level
    /// authentication. The digest is Base64(SHA1(nonce + created + password)), per the OASIS
    /// UsernameToken profile.
    /// </summary>
    public static class WsUsernameToken
    {
        public const string SecurityExtNamespace =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        public const string SecurityUtilityNamespace =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        private const string PasswordDigestType =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest";

        private const string Base64EncodingType =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

        /// <summary>
        /// Writes a Security header carrying a UsernameToken. <paramref name="utcNowOffset"/>
        /// compensates for a device whose clock is known to be wrong.
        /// </summary>
        public static void Write(IXmlWriter writer, string userName, string password, TimeSpan utcNowOffset)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (string.IsNullOrEmpty(userName)) return;

            string nonce = WsDigestAuthentication.CalculateNonce();
            string created = (DateTime.UtcNow + utcNowOffset)
                .ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            string digest = WsDigestAuthentication.CreateSoapDigest(nonce, created, password ?? string.Empty);

            var xml = writer.Xml;
            xml.WriteStartElement("wsse", "Security", SecurityExtNamespace);
            xml.WriteAttributeString("mustUnderstand", OnvifXmlNamespaces.SoapEnvelope, "1");

            xml.WriteStartElement("UsernameToken", SecurityExtNamespace);

            xml.WriteElementString("Username", SecurityExtNamespace, userName);

            xml.WriteStartElement("Password", SecurityExtNamespace);
            xml.WriteAttributeString("Type", PasswordDigestType);
            xml.WriteString(digest);
            xml.WriteEndElement();

            xml.WriteStartElement("Nonce", SecurityExtNamespace);
            xml.WriteAttributeString("EncodingType", Base64EncodingType);
            xml.WriteString(nonce);
            xml.WriteEndElement();

            xml.WriteElementString("Created", SecurityUtilityNamespace, created);

            xml.WriteEndElement();   // UsernameToken
            xml.WriteEndElement();   // Security
        }

        /// <summary>
        /// Reads the UsernameToken out of the Security header of a SOAP envelope, or returns null
        /// when the message carries none. The reader starts anywhere before the envelope element,
        /// and where it ends up is not defined.
        /// </summary>
        public static UsernameToken Read(XmlReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            if (!SoapEnvelope.MoveToEnvelopeChild(reader, "Header")) return null;
            if (reader.IsEmptyElement) return null;

            int headerDepth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == headerDepth) return null;
                if (reader.NodeType != XmlNodeType.Element) continue;

                // The Security element is matched by name alone. Which revision of WS-Security a
                // device declares is not something to refuse its credentials over, and the header
                // has been seen nested rather than sitting directly under Header. The token
                // inside it is matched by namespace as well, because that is what says the
                // elements below it mean what this code reads them as.
                if (reader.LocalName != "Security" || reader.IsEmptyElement) continue;

                UsernameToken token = ReadFromSecurity(reader);
                if (token != null) return token;
            }

            return null;
        }

        /// <summary>
        /// Finds the token inside the Security element the reader is positioned on, and leaves
        /// the reader on that element's end tag when there is none.
        /// </summary>
        private static UsernameToken ReadFromSecurity(XmlReader reader)
        {
            int securityDepth = reader.Depth;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == securityDepth) return null;
                if (reader.NodeType != XmlNodeType.Element) continue;
                if (reader.LocalName != "UsernameToken" || reader.NamespaceURI != SecurityExtNamespace) continue;

                return ReadToken(reader);
            }

            return null;
        }

        /// <summary>Reads the wsse:UsernameToken element the reader is positioned on.</summary>
        private static UsernameToken ReadToken(XmlReader reader)
        {
            var token = new UsernameToken();
            token.Id = reader.GetAttribute("Id", SecurityUtilityNamespace);

            if (reader.IsEmptyElement) return token;

            int tokenDepth = reader.Depth;
            reader.Read();   // step past the start tag

            // Every branch below leaves the reader on the node after what it read, so nothing
            // advances it twice: a loop that read an element and then read again would step over
            // the sibling that follows it, which here is the difference between finding a
            // password and not.
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == tokenDepth) break;

                if (reader.NodeType != XmlNodeType.Element || reader.Depth != tokenDepth + 1)
                {
                    reader.Read();
                    continue;
                }

                if (reader.NamespaceURI == SecurityExtNamespace && reader.LocalName == "Username")
                {
                    token.Username = ReadText(reader);
                }
                else if (reader.NamespaceURI == SecurityExtNamespace && reader.LocalName == "Password")
                {
                    string type = reader.GetAttribute("Type");
                    token.Password = new Password { Type = type, Text = ReadText(reader) };
                }
                else if (reader.NamespaceURI == SecurityExtNamespace && reader.LocalName == "Nonce")
                {
                    string encodingType = reader.GetAttribute("EncodingType");
                    token.Nonce = new Nonce { EncodingType = encodingType, Text = ReadText(reader) };
                }
                else if (reader.NamespaceURI == SecurityUtilityNamespace && reader.LocalName == "Created")
                {
                    token.Created = ReadText(reader);
                }
                else
                {
                    // Devices do add elements the token profile does not describe. One of those
                    // is not a reason to refuse the credentials that came alongside it.
                    reader.Skip();
                }
            }

            return token;
        }

        /// <summary>
        /// Character content of the current element, leaving the reader on the node after it. An
        /// empty element yields an empty string rather than null, so a present but empty element
        /// stays distinguishable from an absent one.
        /// </summary>
        private static string ReadText(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return string.Empty;
            }

            return reader.ReadElementContentAsString();
        }
    }
}
