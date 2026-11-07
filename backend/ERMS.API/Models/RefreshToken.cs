namespace ERMS.API.Models;

/// <summary>
/// Manages cryptographically hashed refresh tokens supporting cookie session lifecycle.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key referencing the associated User.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Secure hash (SHA-256) of the issued opaque refresh token. Raw token is never stored in DB.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Expiration timestamp in UTC.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Timestamp when token was issued in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when token was explicitly revoked/invalidated in UTC.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Returns true if token is currently valid (not revoked and not expired).
    /// </summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;

    // Navigation properties

    /// <summary>
    /// Associated User entity.
    /// </summary>
    public User User { get; set; } = null!;
}
