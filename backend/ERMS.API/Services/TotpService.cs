using System.Security.Cryptography;
using System.Text;
using ERMS.API.Interfaces;
using OtpNet;
using QRCoder;

namespace ERMS.API.Services;

/// <summary>
/// Implements RFC 6238 Time-Based One-Time Password (TOTP) standard operations,
/// QR code image synthesis via QRCoder, and backup recovery code management.
/// </summary>
public class TotpService : ITotpService
{
    private const int SecretKeyByteLength = 20; // 160 bits recommended by RFC 4226 / RFC 6238
    private const int TotpStepSeconds = 30;
    private const int TotpDigits = 6;
    private const string AllowedRecoveryChars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // Base32 Crockford-like safe chars

    /// <summary>
    /// Generates a cryptographically random Base32 TOTP secret key (160 bits / 20 bytes).
    /// </summary>
    public string GenerateSecretKey()
    {
        byte[] secretBytes = KeyGeneration.GenerateRandomKey(SecretKeyByteLength);
        return Base32Encoding.ToString(secretBytes);
    }

    /// <summary>
    /// Generates a standardized otpauth:// URI for authenticator applications.
    /// </summary>
    public string GenerateOtpAuthUri(string email, string secretKey, string issuer = "ERMS")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);

        string label = $"{issuer}:{email}";
        return $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={TotpDigits}&period={TotpStepSeconds}";
    }

    /// <summary>
    /// Generates a PNG QR code rendered as a Data URI (data:image/png;base64,...) from an otpauth URI.
    /// </summary>
    public string GenerateQrCodeDataUri(string otpAuthUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(otpAuthUri);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodeBytes = qrCode.GetGraphic(20);
        string base64String = Convert.ToBase64String(qrCodeBytes);

        return $"data:image/png;base64,{base64String}";
    }

    /// <summary>
    /// Verifies candidate 6-digit TOTP code against the provided Base32 secret key.
    /// Supports a clock drift tolerance window of +/- 1 step (30 seconds).
    /// </summary>
    public bool VerifyTotp(string secretKey, string code)
    {
        if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            byte[] secretBytes = Base32Encoding.ToBytes(secretKey.Trim());
            var totp = new Totp(secretBytes, step: TotpStepSeconds, mode: OtpHashMode.Sha1, totpSize: TotpDigits);

            // Clock drift window: 1 step past, 1 step future (+/- 30 seconds)
            return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a collection of unique, alphanumeric backup recovery codes (e.g., 10 codes formatted like XXXXX-XXXXX).
    /// </summary>
    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>(count);

        for (int i = 0; i < count; i++)
        {
            var part1 = GenerateRandomSegment(5);
            var part2 = GenerateRandomSegment(5);
            codes.Add($"{part1}-{part2}");
        }

        return codes;
    }

    /// <summary>
    /// Computes a SHA-256 hash of a recovery code for secure persistent storage.
    /// </summary>
    public string HashRecoveryCode(string recoveryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recoveryCode);

        // Normalize by trimming and stripping hyphens and converting to uppercase
        string normalized = recoveryCode.Replace("-", "").Trim().ToUpperInvariant();
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string GenerateRandomSegment(int length)
    {
        var buffer = new char[length];
        byte[] randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        for (int i = 0; i < length; i++)
        {
            buffer[i] = AllowedRecoveryChars[randomBytes[i] % AllowedRecoveryChars.Length];
        }

        return new string(buffer);
    }
}
