using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using SS.AuthService.Application.Interfaces;

namespace SS.AuthService.Infrastructure.Services;

/// <summary>
/// Implementasi Argon2id untuk password hashing.
/// Argon2id adalah algoritma pemenang password hashing competition yang tahan terhadap serangan side-channel dan GPU.
/// </summary>
public class Argon2PasswordHasher : IPasswordHasher
{
    // Parameter Argon2id (Bisa disesuaikan di appsettings jika perlu)
    private const int DegreeOfParallelism = 8;
    private const int MemorySize = 128 * 1024; // 128 MB
    private const int Iterations = 4;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = GenerateHash(password, salt);

        // Format: {salt_base64}.{hash_base64}
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var originalHash = Convert.FromBase64String(parts[1]);

        var newHash = GenerateHash(password, salt);

        // Menggunakan CryptographicOperations.FixedTimeEquals untuk mencegah timing attack
        return CryptographicOperations.FixedTimeEquals(originalHash, newHash);
    }

    private byte[] GenerateHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        return argon2.GetBytes(HashSize);
    }
}
