using System.Security.Cryptography;
using System.Text;
using ERMS.API.Interfaces;
using Isopoh.Cryptography.Argon2;

namespace ERMS.API.Services;

/// <summary>
/// Production-grade Argon2id password hashing and verification implementation.
/// Complies with OWASP password storage recommendations:
/// Algorithm: Argon2id (v=19), Memory: 64MB (65536 KB), Iterations: 3, Parallelism: 2, Salt: 16 bytes.
/// </summary>
public class Argon2idPasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int TimeCost = 3;
    private const int MemoryCost = 65536; // 64 MB
    private const int Parallelism = 2;

    /// <summary>
    /// Computes a secure Argon2id cryptographic hash for the given plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>A tuple of (Argon2id encoded hash string, Base64 salt string).</returns>
    public (string Hash, string Salt) HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = new byte[SaltSizeBytes];
        RandomNumberGenerator.Fill(salt);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing, // Argon2id (Hybrid Data-Independent & Data-Dependent)
            Version = Argon2Version.Nineteen,
            TimeCost = TimeCost,
            MemoryCost = MemoryCost,
            Lanes = Parallelism,
            Threads = Parallelism,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = salt
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        string encodedHash = config.EncodeString(hash.Buffer);
        string saltBase64 = Convert.ToBase64String(salt);

        return (encodedHash, saltBase64);
    }

    /// <summary>
    /// Verifies candidate password against stored Argon2 encoded hash.
    /// </summary>
    /// <param name="password">Candidate plain-text password.</param>
    /// <param name="storedHash">Argon2 encoded hash string.</param>
    /// <returns>True if password verifies against hash; false otherwise.</returns>
    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        try
        {
            return Argon2.Verify(storedHash, password);
        }
        catch
        {
            return false;
        }
    }
}
