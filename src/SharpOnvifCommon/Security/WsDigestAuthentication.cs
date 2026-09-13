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
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace SharpOnvifCommon.Security
{
    [XmlRoot(ElementName = "Nonce", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Nonce
    {
        [XmlAttribute(AttributeName = "EncodingType")]
        public string EncodingType { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "Password", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Password
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "UsernameToken", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class UsernameToken
    {
        [XmlElement(ElementName = "Username", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public string Username { get; set; }

        [XmlElement(ElementName = "Password", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public Password Password { get; set; }

        [XmlElement(ElementName = "Nonce", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public Nonce Nonce { get; set; }

        [XmlElement(ElementName = "Created", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
        public string Created { get; set; }

        [XmlAttribute(AttributeName = "Id", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
        public string Id { get; set; }
    }

    public static class WsDigestAuthentication
    {
        public static string CalculateNonce(int length = 32)
        {
            var byteArray = new byte[length];
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(byteArray);
            }
            return Convert.ToBase64String(byteArray);
        }

        public static string CreateSoapDigest(string nonce, string created, string password)
        {
            var nonceBytes = Convert.FromBase64String(nonce);
            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = new byte[createdBytes.Length + nonceBytes.Length + passwordBytes.Length];

            Buffer.BlockCopy(nonceBytes, 0, combined, 0, nonceBytes.Length);
            Buffer.BlockCopy(createdBytes, 0, combined, nonceBytes.Length, createdBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, nonceBytes.Length + createdBytes.Length, passwordBytes.Length);

            using (var sha = SHA1.Create())
            {
                return Convert.ToBase64String(sha.ComputeHash(combined));
            }
        }
    }
}
