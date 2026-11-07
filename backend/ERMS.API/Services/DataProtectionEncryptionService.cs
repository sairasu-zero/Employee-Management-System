using ERMS.API.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace ERMS.API.Services;

/// <summary>
/// Implements sensitive data encryption at rest using ASP.NET Core Data Protection APIs.
/// Provides cryptographic protection (authenticated AES-256-CBC / HMAC-SHA256 or AES-GCM)
/// with automated key lifecycle management.
/// </summary>
public class DataProtectionEncryptionService : IEncryptionService
{
    private const string Purpose = "ERMS.Security.SensitiveDataProtection.v1";
    private readonly IDataProtector _protector;

    public DataProtectionEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        ArgumentNullException.ThrowIfNull(dataProtectionProvider);
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    /// <summary>
    /// Encrypts plain text data into a protected ciphertext string.
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        return _protector.Protect(plainText);
    }

    /// <summary>
    /// Decrypts protected ciphertext back into the original plain text.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText;
        }

        return _protector.Unprotect(cipherText);
    }
}
