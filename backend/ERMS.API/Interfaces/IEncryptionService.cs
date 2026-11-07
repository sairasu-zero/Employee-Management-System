namespace ERMS.API.Interfaces;

/// <summary>
/// Abstraction for cryptographic symmetric encryption and decryption of sensitive data at rest.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plain text data into a protected ciphertext string.
    /// </summary>
    /// <param name="plainText">Sensitive data to protect.</param>
    /// <returns>Protected ciphertext string.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts protected ciphertext back into the original plain text.
    /// </summary>
    /// <param name="cipherText">Protected ciphertext to unprotect.</param>
    /// <returns>Original plain text data.</returns>
    string Decrypt(string cipherText);
}
