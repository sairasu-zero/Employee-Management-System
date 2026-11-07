namespace ERMS.API.Models;

/// <summary>
/// Provides an immutable audit trail entry for security, compliance, and regulatory governance.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key referencing the User who executed the action (null for anonymous/system actions).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Human-readable identity of actor (username, email, or 'System') preserved even if user is deleted.
    /// </summary>
    public string? UserIdentity { get; set; }

    /// <summary>
    /// Type of action performed (e.g., Login, Logout, CreateEmployee, UpdateDepartment, RoleAssigned).
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// High-level module classification (e.g., Authentication, AccessControl, EmployeeManagement, Audit).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Detailed summary describing what occurred, changed values, or event details.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Target resource affected (e.g., Employee/EMP-0001, User/jdoe, Role/HR-Officer).
    /// </summary>
    public string? TargetResource { get; set; }

    /// <summary>
    /// Execution outcome (e.g., Success, Failed, Denied, Error).
    /// </summary>
    public string ActionResult { get; set; } = string.Empty;

    /// <summary>
    /// Client IPv4 or IPv6 address originating the request.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Exact UTC timestamp when the logged action transpired.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Associated User entity if available.
    /// </summary>
    public User? User { get; set; }
}
