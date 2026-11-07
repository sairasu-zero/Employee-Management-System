namespace ERMS.API.Models;

/// <summary>
/// Represents a one-time emergency backup recovery code used to authenticate when TOTP device is lost.
/// </summary>
public class RecoveryCode
{
    /// <summary>
    /// Unique identifier for the recovery code entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key identifier referencing the User account.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Secure hash of the recovery code (SHA-256 / Argon2id). Plain text code is never stored.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this single-use code has already been redeemed.
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// Timestamp when this recovery code was redeemed in UTC.
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Timestamp when this recovery code was generated in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Associated User entity.
    /// </summary>
    public User User { get; set; } = null!;
}
