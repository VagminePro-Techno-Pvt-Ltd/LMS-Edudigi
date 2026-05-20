using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace TMS.Common
{
    public static class SecurityUtility
    {
        /// <summary>
        /// Hashes a password using PBKDF2.
        /// </summary>
        public static string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        /// <summary>
        /// Verifies a password against a PBKDF2 hash.
        /// </summary>
        public static bool VerifyHash(string password, string hashedPassword)
        {
            try
            {
                var parts = hashedPassword.Split('.');
                if (parts.Length != 2) return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                string storedHash = parts[1];

                string hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

                return hashedInput == storedHash;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a string is likely a PBKDF2 hash (contains a dot and base64 parts).
        /// </summary>
        public static bool IsHashed(string password)
        {
            return password.Contains(".") && password.Length > 40;
        }
    }
}
