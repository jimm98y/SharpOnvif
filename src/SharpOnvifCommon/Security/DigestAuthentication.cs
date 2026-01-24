using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SharpOnvifCommon.Security
{
    public enum BinarySerializationType
    {
        Hex,
        Base64
    }

    public static class DigestAuthentication
    {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_NONCE_EMPTY = -1;
        public const int ERROR_NONCE_FORMAT = -2;
        public const int ERROR_NONCE_LENGTH = -3;
        public const int ERROR_NONCE_FUTURE = -4;
        public const int ERROR_NONCE_EXPIRED = -5;
        public const int ERROR_NONCE_INVALID = -6;

        public static byte[] NoncePrivateKey = GenerateRandom();

        public static byte[] GenerateRandom(int length = 32)
        {
            var byteArray = new byte[length];
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(byteArray);
            }
            return byteArray;
        }

        public static string GenerateServerNonce(string algorithm, BinarySerializationType nonceType, DateTimeOffset currentTimestamp, byte[] etag = null, byte[] salt = null)
        {
            long timestamp = currentTimestamp.ToUnixTimeMilliseconds();
            using (var hash = GetHashAlgorithm(algorithm))
            {
                // RFC 2617: nonce includes a timestamp and it is generated from a known private key so that it can be validated
                // time-stamp H(time-stamp ":" ETag ":" private-key)
                byte[] timestampBytes = new byte[] 
                { 
                    (byte)((timestamp >> 56) & 0xff),
                    (byte)((timestamp >> 48) & 0xff),
                    (byte)((timestamp >> 40) & 0xff),
                    (byte)((timestamp >> 32) & 0xff),
                    (byte)((timestamp >> 24) & 0xff),
                    (byte)((timestamp >> 16) & 0xff),
                    (byte)((timestamp >> 8) & 0xff),
                    (byte)((timestamp) & 0xff),
                };

                // we add some salt to the mix to compensate the lack of an etag
                return
                    BytesToString(nonceType,
                        timestampBytes
                        .Concat(salt ?? new byte[0])
                        .Concat(Hash(algorithm, hash, EncodingGetBytes($"{timestamp}:{ToHex(salt)}{ToHex(etag)}:{ToHex(NoncePrivateKey)}")))
                        .ToArray()
                    );
            }
        }

        public static string GenerateClientNonce(BinarySerializationType nonceType, int cnonceLength = 16)
        {
            return BytesToString(nonceType, GenerateRandom(cnonceLength));
        }

        public static int ValidateServerNonce(string algorithm, BinarySerializationType nonceType, string nonce, DateTimeOffset currentTimestamp, byte[] etag = null, int saltLength = 0, int lifetimeMiliseconds = 30000)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                return ERROR_NONCE_EMPTY;
            }

            byte[] nonceBytes = null;

            if (nonceType == BinarySerializationType.Base64)
            {
                try
                {
                    nonceBytes = Convert.FromBase64String(nonce);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Nonce is not valid base64 string");
                    Debug.WriteLine(ex.Message);
                    return ERROR_NONCE_FORMAT;
                }
            }
            else if(nonceType == BinarySerializationType.Hex)
            {
                try
                {
                    nonceBytes = FromHex(nonce);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Nonce is not valid hexadecimal string");
                    Debug.WriteLine(ex.Message);
                    return ERROR_NONCE_FORMAT;
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            if (nonceBytes.Length <= 8 + saltLength) // 8 byte timestamp + salt
            {
                return ERROR_NONCE_LENGTH;
            }

            long timestampNonce =
                (long)nonceBytes[0] << 56 |
                (long)nonceBytes[1] << 48 |
                (long)nonceBytes[2] << 40 |
                (long)nonceBytes[3] << 32 |
                (long)nonceBytes[4] << 24 |
                (long)nonceBytes[5] << 16 |
                (long)nonceBytes[6] << 8 |
                (long)nonceBytes[7];

            DateTimeOffset nonceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestampNonce);
            if (currentTimestamp.CompareTo(nonceDateTime) < 0)
            {
                Debug.WriteLine("Nonce is from the future");
                return ERROR_NONCE_FUTURE;
            }

            if (currentTimestamp.Subtract(nonceDateTime).TotalMilliseconds >= lifetimeMiliseconds)
            {
                Debug.WriteLine("Nonce is expired");
                return ERROR_NONCE_EXPIRED;
            }

            byte[] salt = nonceBytes.Skip(8).Take(saltLength).ToArray();

            string generatedNonce = GenerateServerNonce(algorithm, nonceType, nonceDateTime, etag, salt);
            if(string.Compare(generatedNonce, nonce) != 0)
            {
                Debug.WriteLine("Nonce is invalid");
                return ERROR_NONCE_INVALID;
            }

            return 0;
        }

        public static string CreateWebDigestRFC2069(string algorithm, string userName, string realm, string password, string nonce, string method, string uri)
        {
            using (var hash = GetHashAlgorithm(algorithm))
            {
                string HA1 = ToHex(Hash(algorithm, hash, EncodingGetBytes($"{userName}:{realm}:{password}")));
                string HA2 = ToHex(Hash(algorithm, hash, EncodingGetBytes($"{method}:{uri}")));
                string digest = ToHex(Hash(algorithm, hash, EncodingGetBytes($"{HA1}:{nonce}:{HA2}")));
                return digest;
            }
        }

        public static string CreateWebDigestRFC2617(string algorithm, string userName, string realm, string password, string nonce, string method, string uri, int nc, string cnonce, string qop, string noncePrime = null, string cnoncePrime = null, string bodyHash = null)
        {
            using (var hash = GetHashAlgorithm(algorithm))
            {
                var bbb = EncodingGetBytes($"{userName}:{realm}:{password}");
                string HA1 = ToHex(Hash(algorithm, hash, bbb));

                if (algorithm.EndsWith("-sess", StringComparison.OrdinalIgnoreCase))
                {
                    if (noncePrime == null)
                    {
                        throw new ArgumentNullException(nameof(noncePrime));
                    }
                    if (cnoncePrime == null)
                    {
                        throw new ArgumentNullException(nameof(noncePrime));
                    }

                    HA1 = ToHex(Hash(algorithm, hash, EncodingGetBytes($"{HA1}:{noncePrime}:{cnoncePrime}")));
                }
                    
                string HA2;

                if(string.IsNullOrEmpty(qop) || string.Compare(qop, "auth", true) == 0)
                {
                    HA2 = ToHex(Hash(algorithm, hash, EncodingGetBytes($"{method}:{uri}")));
                }
                else if (string.Compare(qop, "auth-int", true) == 0)
                {
                    if (bodyHash == null)
                    {
                        throw new ArgumentNullException(nameof(bodyHash));
                    }

                    HA2 = ToHex(Hash(algorithm, hash, EncodingGetBytes($"{method}:{uri}:{bodyHash}")));
                }
                else
                {
                    throw new NotSupportedException(nameof(qop));
                }
                                
                return ToHex(Hash(algorithm, hash, EncodingGetBytes($"{HA1}:{nonce}:{ConvertIntToNC(nc)}:{cnonce}:{qop}:{HA2}")));
            }
        }

        public static string CreateWwwAuthenticateRFC2069(
            string algorithm, 
            BinarySerializationType binarySerialization,
            DateTimeOffset currentTimestamp, 
            byte[] etag,
            byte[] salt,
            string realm,
            bool isStale = false)
        {
            string serverNonce = GenerateServerNonce(algorithm, binarySerialization, currentTimestamp, etag, salt);
            return $"Digest realm=\"{realm}\", nonce=\"{serverNonce}\", stale=\"{isStale.ToString().ToUpperInvariant()}\"";
        }

        public static string CreateWwwAuthenticateRFC2617(
            string nonceAlgorithm, 
            BinarySerializationType nonceSerialization, 
            string algorithm,
            DateTimeOffset currentTimestamp, 
            byte[] etag, 
            byte[] salt, 
            string realm, 
            string opaque = "00000000", 
            string qop = "auth, auth-int",
            bool isStale = false)
        {
            string serverNonce = GenerateServerNonce(nonceAlgorithm, nonceSerialization, currentTimestamp, etag, salt);

            // optional
            string responseAlgorithm = (string.IsNullOrEmpty(algorithm) || algorithm == "MD5") ? "" : $", algorithm={algorithm}";

            return $"Digest realm=\"{realm}\", qop=\"{qop}\"{responseAlgorithm}, nonce=\"{serverNonce}\", opaque=\"{opaque}\", stale=\"{isStale.ToString().ToUpperInvariant()}\"";
        }

        public static string CreateWwwAuthenticateRFC7616(
            string nonceAlgorithm,
            BinarySerializationType nonceSerialization,
            string algorithm,
            DateTimeOffset currentTimestamp,
            byte[] etag,
            byte[] salt,
            string realm,
            string opaque = "00000000",
            string qop = "auth, auth-int",
            string charset = "UTF-8",
            bool userhash = false,
            bool isStale = false)
        {
            string serverNonce = GenerateServerNonce(nonceAlgorithm, nonceSerialization, currentTimestamp, etag, salt);

            // optional
            string responseAlgorithm = (string.IsNullOrEmpty(algorithm) || algorithm == "MD5") ? "" : $", algorithm={algorithm}";
            string responseCharset = string.IsNullOrEmpty(charset) ? "" : $", charset={charset}"; // UTF-8
            string responseUserhash = userhash ? $", userhash={userhash.ToString().ToUpperInvariant()}" : ""; // see CreateUserNameHashRFC7616

            return $"Digest realm=\"{realm}\", qop=\"{qop}\"{responseAlgorithm}, nonce=\"{serverNonce}\", opaque=\"{opaque}\"{responseCharset}{responseUserhash}, stale=\"{isStale.ToString().ToUpperInvariant()}\"";
        }

        public static string CreateUserNameHashRFC7616(string algorithm, string userName, string realm)
        {
            using (var hash = GetHashAlgorithm(algorithm))
            {
                // username = H( unq(username) ":" unq(realm) )
                // TODO: store this in the users database and use it for lookups
                return ToHex(Hash(algorithm, hash, EncodingGetBytes($"{userName}:{realm}")));
            }
        }

        public static string CreateAuthorizationRFC2069(string userName, string realm, string nonce, string uri, string response, string opaque)
        {
            string responseOpaque = string.IsNullOrEmpty(opaque) ? "" : $", opaque=\"{opaque}\"";
            return $"Digest username=\"{userName}\", realm=\"{realm}\", nonce=\"{nonce}\", uri=\"{uri}\", response=\"{response}\"{responseOpaque}";
        }

        public static string CreateAuthorizationRFC2617(string userName, string realm, string nonce, string uri, string response, string opaque, string algorithm, string qop, int nc, string cnonce)
        {
            string responseOpaque = string.IsNullOrEmpty(opaque) ? "" : $", opaque=\"{opaque}\"";
            string responseAlgorithm = string.IsNullOrEmpty(algorithm) || algorithm == "MD5" ? "" : $", algorithm=\"{algorithm}\"";
            return $"Digest username=\"{userName}\", realm=\"{realm}\", uri=\"{uri}\"{responseAlgorithm}, nonce=\"{nonce}\", qop={qop}, nc={ConvertIntToNC(nc)}, cnonce=\"{cnonce}\", response=\"{response}\"{responseOpaque}";
        }

        public static string CreateAuthorizationRFC7616(string userName, string realm, string nonce, string uri, string response, string opaque, string algorithm, string qop, int nc, string cnonce, bool userhash)
        {
            string responseOpaque = string.IsNullOrEmpty(opaque) ? "" : $", opaque=\"{opaque}\"";
            string responseAlgorithm = string.IsNullOrEmpty(algorithm) || algorithm == "MD5" ? "" : $", algorithm=\"{algorithm}\"";
            string responseUserhash = userhash ? $", userhash={userhash.ToString().ToLowerInvariant()}" : "";
            string responseUserName;
            string escapedUserName = Uri.EscapeDataString(userName);
            if (escapedUserName.CompareTo(userName) != 0)
            {
                responseUserName = $"username*=UTF-8''{escapedUserName}";
            }
            else
            {
                responseUserName = $"username=\"{userName}\"";
            }
            return $"Digest {responseUserName}, realm=\"{realm}\", uri=\"{uri}\"{responseAlgorithm}, nonce=\"{nonce}\", qop={qop}, nc={ConvertIntToNC(nc)}, cnonce=\"{cnonce}\", response=\"{response}\"{responseOpaque}{responseUserhash}";
        }

        public static int ConvertNCToInt(string nc)
        {
            if (string.IsNullOrEmpty(nc) || nc.Length != 8)
            {
                throw new ArgumentException(nameof(nc));
            }

            byte[] ncBytes = FromHex(nc);
            int ret =
                 (ncBytes[0] << 24) |
                 (ncBytes[1] << 16) |
                 (ncBytes[2] << 8) |
                 (ncBytes[3]);
            return ret;
        }

        public static string ConvertIntToNC(int nc)
        {
            byte[] ncBytes = new byte[]
            {
                (byte)((nc >> 24) & 0xff),
                (byte)((nc >> 16) & 0xff),
                (byte)((nc >> 8) & 0xff),
                (byte)(nc & 0xff),
            };
            return ToHex(ncBytes);
        }

        private static HashAlgorithm GetHashAlgorithm(string algorithm)
        {
            switch (algorithm?.ToUpperInvariant())
            {
                case "SHA-512-256":
                case "SHA-512-256-SESS":
                    return SHA512.Create();

                case "SHA-256":
                case "SHA-256-SESS":
                    return SHA256.Create();

                case "MD5":
                case "MD5-SESS":
                default:
                    return MD5.Create();
            }
        }

        private static int GetHashLength(string algorithm)
        {
            switch (algorithm?.ToUpperInvariant())
            {
                case "SHA-512-256":
                case "SHA-512-256-SESS":
                    return 32;

                case "SHA-256":
                case "SHA-256-SESS":
                    return 32;

                case "MD5":
                case "MD5-SESS":
                default:
                    return 16;
            }
        }

        private static byte[] Hash(string algorithm, HashAlgorithm hash, byte[] input)
        {
            var result = hash.ComputeHash(input);
            int length = GetHashLength(algorithm); 

            if(result.Length > length)
            {
                return result.Take(length).ToArray(); // SHA-512-256 - 512 bit digest truncated to 256 bit
            }
            else
            {
                return result;
            }
        }

        private static string ToHex(byte[] input)
        {
            if (input == null) 
                return string.Empty;
            else
                return string.Concat(input.Select(x => x.ToString("x2")));
        }

        private static byte[] FromHex(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static byte[] EncodingGetBytes(string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        private static string BytesToString(BinarySerializationType serialization, byte[] input)
        {
            switch (serialization)
            {
                case BinarySerializationType.Base64:
                    return Convert.ToBase64String(input);

                case BinarySerializationType.Hex:
                    return ToHex(input);

                default:
                    throw new NotSupportedException(serialization.ToString());
            }
        }
    }
}
