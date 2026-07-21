using Cortex.DTOs;

namespace Cortex.Services.Interfaces
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        (bool IsValid, string? ErrorMessage) ValidatePassword(string password);
    }
}