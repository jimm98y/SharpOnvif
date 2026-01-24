using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SharpOnvifCommon.Security
{
    public static class DigestHelpers
    {
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

        public static string GenerateServerNonce(DateTimeOffset currentTimestamp, byte[] etag = null, byte[] salt = null)
        {
            long timestamp = currentTimestamp.ToUnixTimeMilliseconds();
            using (MD5 hash = MD5.Create())
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
                return Convert.ToBase64String(
                    timestampBytes
                    .Concat(salt ?? new byte[0])
                    .Concat(Hash(hash, Encoding.UTF8.GetBytes($"{timestamp}:{Hex(salt)}{Hex(etag)}:{Hex(NoncePrivateKey)}")))
                    .ToArray()
                );
            }
        }

        public static int ValidateServerNonce(string nonce, DateTimeOffset currentTimestamp, byte[] etag = null, int saltLength = 0, int lifetimeMiliseconds = 30000)
        {
            if (string.IsNullOrEmpty(nonce))
            {
                return -1;
            }

            byte[] nonceBytes = null;

            try
            {
                nonceBytes = Convert.FromBase64String(nonce);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Nonce is not valid base64");
                Debug.WriteLine(ex.Message);
                return -2;
            }

            if(nonceBytes.Length <= 8 + saltLength) // 8 byte timestamp + salt
            {
                return -3;
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
                return -4;
            }

            if (currentTimestamp.Subtract(nonceDateTime).TotalMilliseconds >= lifetimeMiliseconds)
            {
                Debug.WriteLine("Nonce is expired");
                return -5;
            }

            byte[] salt = nonceBytes.Skip(8).Take(saltLength).ToArray();

            string generatedNonce = GenerateServerNonce(nonceDateTime, etag, salt);
            if(string.Compare(generatedNonce, nonce) != 0)
            {
                Debug.WriteLine("Nonce is invalid");
                return -6;
            }

            return 0;
        }

        public static string CreateWebDigestRFC2069(string userName, string realm, string password, string nonce, string method, string uri)
        {
            using (MD5 hash = MD5.Create())
            {
                string HA1 = Hex(Hash(hash, Encoding.UTF8.GetBytes($"{userName}:{realm}:{password}")));
                string HA2 = Hex(Hash(hash, Encoding.UTF8.GetBytes($"{method}:{uri}")));
                string digest = Hex(Hash(hash, Encoding.UTF8.GetBytes($"{HA1}:{nonce}:{HA2}")));
                return digest;
            }
        }

        private static byte[] Hash(HashAlgorithm hash, byte[] input)
        {
            return hash.ComputeHash(input);
        }

        private static string Hex(byte[] input)
        {
            if (input == null) 
                return string.Empty;
            else
                return string.Concat(input.Select(x => x.ToString("x2")));
        }
    }
}
