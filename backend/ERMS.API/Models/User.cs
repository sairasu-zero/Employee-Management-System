namespace ERMS.API.Models;

/// <summary>
/// Represents an authenticated system user account with access credentials and security settings.
/// </summary>
public class User
{
    /// <summary>
    /// Unique identifier for the user account.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Optional foreign key link to an Employee record. Null for pure admin accounts.
    /// </summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>
    /// Unique username used for credential authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Unique primary email address associated with the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Argon2id cryptographic password hash. Never exposed in API contracts.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Cryptographic salt applied during password hashing.
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAtUtc { get; set; }

    public int SessionVersion { get; set; }

    /// <summary>
    /// Primary or default role identifier.
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// Indicates whether the user account is active and permitted to authenticate.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Enforces multi-factor authentication (TOTP) requirement upon login.
    /// </summary>
    public bool RequireMfa { get; set; } = false;

    /// <summary>
    /// Timestamp of most recent successful login in UTC.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Number of consecutive failed password verification attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// Timestamp until which account is locked due to brute-force protection in UTC.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Timestamp when account was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when account profile was last modified in UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated workforce employee profile if linked.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Primary role assigned to the user.
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// Many-to-many relationship join collection to assigned system roles.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// TOTP Multi-factor authentication enrollment configuration.
    /// </summary>
    public MfaConfiguration? MfaConfiguration { get; set; }

    /// <summary>
    /// Single-use emergency recovery backup codes.
    /// </summary>
    public ICollection<RecoveryCode> RecoveryCodes { get; set; } = new List<RecoveryCode>();

    /// <summary>
    /// Active and historic refresh tokens associated with user sessions.
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Audit log activities executed by this user account.
    /// </summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
