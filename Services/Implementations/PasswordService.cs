using BCrypt.Net;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Implementations
{
    public class PasswordService : IPasswordService
    {
        // BCrypt cost factor - 12 is recommended as of 2024 for good security/performance balance
        private const int BcryptWorkFactor = 12;

        // Password policy constants
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            // BCrypt automatically generates a salt and includes it in the hash
            return BCrypt.Net.BCrypt.EnhancedHashPassword(password, BcryptWorkFactor, HashType.SHA384);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash, HashType.SHA384);
            }
            catch
            {
                // If verification fails for any reason (malformed hash, etc.), return false
                return false;
            }
        }

        public (bool IsValid, string? ErrorMessage) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required");

            if (password.Length < MinPasswordLength)
                return (false, $"Password must be at least {MinPasswordLength} characters long");

            if (password.Length > MaxPasswordLength)
                return (false, $"Password must not exceed {MaxPasswordLength} characters");

            // Check for uppercase letter
            if (!password.Any(char.IsUpper))
                return (false, "Password must contain at least one uppercase letter");

            // Check for lowercase letter
            if (!password.Any(char.IsLower))
                return (false, "Password must contain at least one lowercase letter");

            // Check for digit
            if (!password.Any(char.IsDigit))
                return (false, "Password must contain at least one number");

            // Check for special character
            var specialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            if (!password.Any(c => specialChars.Contains(c)))
                return (false, "Password must contain at least one special character");

            return (true, null);
        }
    }
}