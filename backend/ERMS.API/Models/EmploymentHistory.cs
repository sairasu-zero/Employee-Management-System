namespace ERMS.API.Models;

/// <summary>
/// Records changes in an employee's role, department, or employment status over time.
/// </summary>
public class EmploymentHistory
{
    /// <summary>
    /// Unique identifier for the historical event.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key identifier referencing the Employee.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Prior job title/position before the change occurred.
    /// </summary>
    public string? PreviousPosition { get; set; }

    /// <summary>
    /// New job title/position effective from change date.
    /// </summary>
    public string NewPosition { get; set; } = string.Empty;

    /// <summary>
    /// Prior department assignment before the change.
    /// </summary>
    public Guid? PreviousDepartmentId { get; set; }

    /// <summary>
    /// New department assignment following the change.
    /// </summary>
    public Guid? NewDepartmentId { get; set; }

    /// <summary>
    /// Type of movement (e.g., Hire, Promotion, Transfer, Demotion, TitleChange, Reassignment).
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Effective date of career change in UTC.
    /// </summary>
    public DateTime ChangeDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Justification, administrative notes, or performance context for this movement.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Name or identifier of authorizing manager or HR officer.
    /// </summary>
    public string? ApprovedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Employee subject to this historical event.
    /// </summary>
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Prior department reference (if any).
    /// </summary>
    public Department? PreviousDepartment { get; set; }

    /// <summary>
    /// Destination department reference (if any).
    /// </summary>
    public Department? NewDepartment { get; set; }
}
