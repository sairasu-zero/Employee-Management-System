namespace ERMS.API.Interfaces;

/// <summary>
/// Service interface for Time-Based One-Time Password (TOTP) generation, verification, and QR code provisioning.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates a cryptographically random Base32 TOTP secret key (160 bits / 20 bytes).
    /// </summary>
    /// <returns>Base32 encoded secret key.</returns>
    string GenerateSecretKey();

    /// <summary>
    /// Generates a standardized otpauth:// URI for authenticator applications (Google Authenticator, Microsoft Authenticator).
    /// </summary>
    /// <param name="email">User's account email address.</param>
    /// <param name="secretKey">Base32 encoded secret key.</param>
    /// <param name="issuer">Issuer name (defaults to "ERMS").</param>
    /// <returns>Formulated otpauth URI.</returns>
    string GenerateOtpAuthUri(string email, string secretKey, string issuer = "ERMS");

    /// <summary>
    /// Generates a PNG QR code rendered as a Data URI (data:image/png;base64,...) from an otpauth URI.
    /// </summary>
    /// <param name="otpAuthUri">The otpauth URI string.</param>
    /// <returns>Data URI containing the Base64 PNG QR code.</returns>
    string GenerateQrCodeDataUri(string otpAuthUri);

    /// <summary>
    /// Verifies candidate 6-digit TOTP code against the provided Base32 secret key.
    /// Supports a clock drift tolerance window of +/- 1 step (30 seconds).
    /// </summary>
    /// <param name="secretKey">Base32 encoded secret key.</param>
    /// <param name="code">6-digit TOTP token string submitted by user.</param>
    /// <returns>True if code is valid; otherwise false.</returns>
    bool VerifyTotp(string secretKey, string code);

    /// <summary>
    /// Generates a collection of unique, alphanumeric backup recovery codes (e.g., 10 codes formatted like XXXXX-XXXXX).
    /// </summary>
    /// <param name="count">Number of recovery codes to generate (default 10).</param>
    /// <returns>List of plain-text recovery codes.</returns>
    List<string> GenerateRecoveryCodes(int count = 10);

    /// <summary>
    /// Computes a SHA-256 hash of a recovery code for secure persistent storage.
    /// </summary>
    /// <param name="recoveryCode">Plain text recovery code.</param>
    /// <returns>Hex-encoded SHA-256 hash string.</returns>
    string HashRecoveryCode(string recoveryCode);
}
