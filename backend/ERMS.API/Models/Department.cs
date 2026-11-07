namespace ERMS.API.Models;

/// <summary>
/// Represents an organizational division, unit, or department within the enterprise.
/// </summary>
public class Department
{
    /// <summary>
    /// Unique identifier for the department.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Formal title of the department (e.g., Information Technology, Human Resources, Finance).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique organizational code (e.g., IT, HR, FIN, OPS).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of departmental mandate or functions.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional foreign key pointing to the Employee designated as department head/manager.
    /// </summary>
    public Guid? ManagerEmployeeId { get; set; }

    /// <summary>
    /// Timestamp when department record was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// The Employee assigned as the manager/head of this department.
    /// </summary>
    public Employee? ManagerEmployee { get; set; }

    /// <summary>
    /// Employees currently assigned to this department.
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    /// <summary>
    /// Employment histories referencing this department as previous assignment.
    /// </summary>
    public ICollection<EmploymentHistory> PreviousDepartmentHistories { get; set; } = new List<EmploymentHistory>();

    /// <summary>
    /// Employment histories referencing this department as new destination assignment.
    /// </summary>
    public ICollection<EmploymentHistory> NewDepartmentHistories { get; set; } = new List<EmploymentHistory>();
}
