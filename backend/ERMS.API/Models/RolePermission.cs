namespace ERMS.API.Models;

/// <summary>
/// Join entity representing granular permission assignments granted to a Role.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Foreign key identifier referencing the Role.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Foreign key identifier referencing the Permission.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Timestamp when the permission was granted to the role in UTC.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier or username of the administrator who granted the permission.
    /// </summary>
    public string? GrantedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated Role entity.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Associated Permission entity.
    /// </summary>
    public Permission Permission { get; set; } = null!;
}
