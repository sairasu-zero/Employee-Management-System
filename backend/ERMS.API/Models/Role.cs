namespace ERMS.API.Models;

/// <summary>
/// Defines an authorization role within the Role-Based Access Control (RBAC) subsystem.
/// </summary>
public class Role
{
    /// <summary>
    /// Unique identifier for the role.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique administrative name of the role (e.g., System Administrator, HR Officer, Department Head, Standard Employee).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive summary of the duties and authorization scope for this role.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this role is an immutable system role that cannot be deleted or renamed.
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Timestamp when role was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Users assigned to this role via the UserRoles join entity.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Granular permissions granted to this role.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Users referencing this role as their primary RoleId.
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
