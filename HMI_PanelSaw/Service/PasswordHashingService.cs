using System;
using System.Data.SQLite;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HMI_PanelSaw.Service
{
    public static class PasswordHashingService
    {
        private const int SaltSize = 32;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        private const int MinPasswordLength = 6;
        private const int MaxPasswordLength = 128;

        public class PasswordValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }

            public static PasswordValidationResult Success() => new PasswordValidationResult { IsValid = true };
            public static PasswordValidationResult Failure(string error) => new PasswordValidationResult { IsValid = false, ErrorMessage = error };
        }

        public class HashedPassword
        {
            public string Hash { get; set; }
            public string Salt { get; set; }
            public int Iterations { get; set; }

            public string ToStorageString()
            {
                return $"{Iterations}:{Salt}:{Hash}";
            }
            public static HashedPassword FromStorageString(string storageString)
            {
                if (string.IsNullOrEmpty(storageString))
                    return null;
                var parts = storageString.Split(':');
                if (parts.Length != 3)
                    return null;
                if (!int.TryParse(parts[0], out int iterations))
                    return null;

                return new HashedPassword
                {
                    Iterations = iterations,
                    Salt = parts[1],
                    Hash = parts[2]
                };
            }
        }

        public static PasswordValidationResult ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return PasswordValidationResult.Failure("Password cannot be empty");
            if (password.Length < MinPasswordLength)
                return PasswordValidationResult.Failure($"Password must be at least {MinPasswordLength} characters long.");
            if (password.Length > MaxPasswordLength)
                return PasswordValidationResult.Failure($"Password cannot exceed {MaxPasswordLength} characters.");
            if (!Regex.IsMatch(password, @"^(?=.*[a-zA-Z])(?=.*\d).+$"))
                return PasswordValidationResult.Failure("Password must contain at least one letter and one number.");

            var weakPasswords = new[] {"123456", "password", "admin", "1234", "12345", "123456789", "qwerty"};
            foreach (var weak in weakPasswords)
            {
                if (string.Equals(password, weak, StringComparison.OrdinalIgnoreCase))
                    return PasswordValidationResult.Failure("Password is to common and easily guessable. Please choose a stronger password");
            }
            return PasswordValidationResult.Success();
        }

        public static HashedPassword HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
           byte[] hashBytes = HashPasswordWithSalt(password, saltBytes, Iterations);

            return new HashedPassword
            {
                Hash = Convert.ToBase64String(hashBytes),
                Salt = Convert.ToBase64String(saltBytes),
                Iterations = Iterations
            };
        }
        public static bool VerifyPassword(string password, HashedPassword storedPassword)
        {
            if (string.IsNullOrEmpty(password) || storedPassword == null)
                return false;
            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedPassword.Salt);
                byte[] storedHashBytes = Convert.FromBase64String(storedPassword.Hash);

                byte[] computedHashBytes = HashPasswordWithSalt(password, saltBytes, storedPassword.Iterations);

                return ConstantTimeEquals(storedHashBytes, computedHashBytes);
            }
            catch(Exception)
            {
                return false;
            }
        }
        public static bool VerifyPassword(string password, string storedPasswordString)
        {
            var storedPassword = HashedPassword.FromStorageString(storedPasswordString);
            return VerifyPassword(password, storedPassword);
        }

        public static bool NeedsRehash(HashedPassword storedPassword)
        {
            return storedPassword?.Iterations < Iterations;
        }
        public static bool NeedsRehash(string storedPasswordString)
        {
            var storedPassword = HashedPassword.FromStorageString(storedPasswordString);
            return NeedsRehash(storedPassword);
        }

        public static bool IsLegacyHash(string hash)
        {
            // Legacy hashes are pure base64 strings without the iteration:salt:hash format
            return !string.IsNullOrEmpty(hash) && !hash.Contains(":");
        }

        private static byte[] HashPasswordWithSalt(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
