namespace ERMS.API.Models;

/// <summary>
/// Defines an atomic permission governing specific actions on domain modules.
/// </summary>
public class Permission
{
    /// <summary>
    /// Unique identifier for the permission.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Functional module or subsystem governed by this permission (e.g., Employees, Departments, Users, Roles, AuditLogs).
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Specific operation or action permitted (e.g., Read, Create, Update, Delete, Export, Manage).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Unique qualified permission identifier name (e.g., Employees.Read, Employees.Create, Users.Manage).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Explanatory description of what authorization this permission confers.
    /// </summary>
    public string? Description { get; set; }

    // Navigation properties

    /// <summary>
    /// Role-permission mappings that include this permission.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
