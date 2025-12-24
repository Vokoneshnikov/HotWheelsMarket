using System;
using System.Security.Cryptography;
using System.Text;

namespace HwGarage.Core.Auth
{
    public static class Hashing
    {
        private const int SaltSize = 16;
        
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combined = new byte[saltBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);

            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(combined);

            byte[] saltedHash = new byte[saltBytes.Length + hashBytes.Length];
            Buffer.BlockCopy(saltBytes, 0, saltedHash, 0, saltBytes.Length);
            Buffer.BlockCopy(hashBytes, 0, saltedHash, saltBytes.Length, hashBytes.Length);

            return Convert.ToBase64String(saltedHash);
        }
        
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            byte[] saltedHashBytes = Convert.FromBase64String(storedHash);

            if (saltedHashBytes.Length < SaltSize)
                return false;

            byte[] saltBytes = new byte[SaltSize];
            Buffer.BlockCopy(saltedHashBytes, 0, saltBytes, 0, SaltSize);

            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combined = new byte[saltBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);

            using var sha = SHA256.Create();
            byte[] computedHash = sha.ComputeHash(combined);

            byte[] originalHash = new byte[saltedHashBytes.Length - SaltSize];
            Buffer.BlockCopy(saltedHashBytes, SaltSize, originalHash, 0, originalHash.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, originalHash);
        }
    }
}
