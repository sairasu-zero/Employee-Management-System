namespace ERMS.API.Interfaces;

/// <summary>
/// Cryptographic password hashing and verification abstraction employing the Argon2id algorithm.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Computes a secure Argon2id cryptographic hash for the given plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>A tuple consisting of the Argon2id encoded hash string and the Base64 representation of the salt.</returns>
    (string Hash, string Salt) HashPassword(string password);

    /// <summary>
    /// Verifies a candidate plain-text password against the stored Argon2id hash string.
    /// </summary>
    /// <param name="password">Candidate plain-text password.</param>
    /// <param name="storedHash">Argon2id encoded hash string stored in the database.</param>
    /// <returns>True if password matches; otherwise false.</returns>
    bool VerifyPassword(string password, string storedHash);
}
