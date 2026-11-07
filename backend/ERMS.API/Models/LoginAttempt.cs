namespace ERMS.API.Models;

/// <summary>
/// Records authentication attempts for brute-force monitoring, intrusion detection, and lockout analysis.
/// </summary>
public class LoginAttempt
{
    /// <summary>
    /// Unique identifier for the login attempt.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Submitted identifier (username or email).
    /// </summary>
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether credentials and second factor passed successfully.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Reason recorded on authentication failure (e.g., InvalidCredentials, AccountLocked, InvalidTotp, AccountInactive).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Client IP address initiating the login attempt.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Exact UTC timestamp when the attempt occurred.
    /// </summary>
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
