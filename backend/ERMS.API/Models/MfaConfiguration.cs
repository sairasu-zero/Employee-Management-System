namespace ERMS.API.Models;

/// <summary>
/// Stores Time-Based One-Time Password (TOTP) configuration and state for a User.
/// </summary>
public class MfaConfiguration
{
    /// <summary>
    /// Unique identifier for the MFA configuration record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key identifier referencing the User account.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Cryptographically encrypted TOTP Base32 secret key.
    /// Encrypted at rest using ASP.NET Core Data Protection / AES-GCM.
    /// </summary>
    public string SecretEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Whether TOTP MFA is actively enforced for authentication challenges.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Timestamp when MFA enrollment was finalized in UTC.
    /// </summary>
    public DateTime? EnrolledAt { get; set; }

    /// <summary>
    /// Timestamp when TOTP was last verified successfully in UTC.
    /// </summary>
    public DateTime? LastVerifiedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated User entity.
    /// </summary>
    public User User { get; set; } = null!;
}
