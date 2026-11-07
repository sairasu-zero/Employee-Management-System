namespace ERMS.API.Models;

/// <summary>
/// Join entity representing the many-to-many relationship between Users and Roles.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Foreign key identifier referencing the User.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Foreign key identifier referencing the Role.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Timestamp when role was assigned in UTC.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier or username of the administrator who assigned this role.
    /// </summary>
    public string? AssignedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated User entity.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Associated Role entity.
    /// </summary>
    public Role Role { get; set; } = null!;
}
