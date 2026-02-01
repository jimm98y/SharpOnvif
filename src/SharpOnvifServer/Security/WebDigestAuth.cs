using System;

namespace SharpOnvifServer.Security
{
    public class WebDigestAuth
    {
        public string Response { get; }
        public string UserName { get; }
        public string Realm { get; }
        public string Nonce { get; }
        public string Uri { get; }
        public string Algorithm { get; }
        public string Opaque { get; }

        public string Qop { get; }
        public string CNonce { get; }
        public string Nc { get; }
        public string UserHash { get; }

        public WebDigestAuth(
            string algorithm,
            string userName,
            string realm,
            string nonce,
            string uri,
            string response,
            string qop,
            string cnonce,
            string nc,
            string userHash,
            string opaque)
        {
            Algorithm = string.IsNullOrEmpty(algorithm) ? "" : algorithm;
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            Realm = realm ?? throw new ArgumentNullException(nameof(realm));
            Nonce = nonce ?? throw new ArgumentNullException(nameof(nonce));
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Response = response ?? throw new ArgumentNullException(nameof(response));
            Opaque = opaque;

            Qop = qop;
            CNonce = cnonce;
            Nc = nc;
            UserHash = userHash;
        }
    }
}
