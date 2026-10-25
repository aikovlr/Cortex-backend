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
                throw new ArgumentException("A senha não pode ser vazia.", nameof(password));

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
                return (false, "A senha é obrigatória.");

            if (password.Length < MinPasswordLength)
                return (false, $"A senha deve ter no mínimo {MinPasswordLength} caracteres.");

            if (password.Length > MaxPasswordLength)
                return (false, $"A senha deve ter no máximo {MaxPasswordLength} caracteres.");

            // Check for uppercase letter
            if (!password.Any(char.IsUpper))
                return (false, "A senha deve conter pelo menos uma letra maiúscula.");

            // Check for lowercase letter
            if (!password.Any(char.IsLower))
                return (false, "A senha deve conter pelo menos uma letra minúscula.");

            // Check for digit
            if (!password.Any(char.IsDigit))
                return (false, "A senha deve conter pelo menos um número.");

            // Check for special character
            var specialChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            if (!password.Any(c => specialChars.Contains(c)))
                return (false, "A senha deve conter pelo menos um caractere especial.");

            return (true, null);
        }
    }
}