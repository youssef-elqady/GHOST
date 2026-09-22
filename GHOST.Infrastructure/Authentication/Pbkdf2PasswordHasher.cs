using System.Security.Cryptography;
using GHOST.Application.Authentication;

namespace GHOST.Infrastructure.Authentication;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 600_000;
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, 32);
        return $"PBKDF2-SHA512${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2-SHA512" || !int.TryParse(parts[1], out var iterations)) return false;
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(parts[2]), iterations, HashAlgorithmName.SHA512, 32);
        return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(parts[3]));
    }
}
