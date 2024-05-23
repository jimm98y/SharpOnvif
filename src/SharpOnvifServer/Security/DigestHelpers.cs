using System;
using System.Security.Cryptography;
using System.Text;

namespace SharpOnvifServer.Security
{
    public static class DigestHelpers
    {
        public static string CalculateNonce()
        {
            var byteArray = new byte[32];
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(byteArray);
            }
            return Convert.ToBase64String(byteArray);
        }

        public static string DateTimeToString(System.DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public static string CreateHashedPassword(string nonce, System.DateTime created, string password)
        {
            return CreateHashedPassword(nonce, DateTimeToString(created), password);
        }

        public static string CreateHashedPassword(string nonce, string created, string password)
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
