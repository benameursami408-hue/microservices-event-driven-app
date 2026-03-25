using AuthService.Application.Interfaces;

namespace AuthService.Infrastructure.Authentication;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
    }

    public bool Verify(string passwordHash, string inputPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(inputPassword, passwordHash);
    }
}
