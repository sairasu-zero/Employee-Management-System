namespace ERMS.API.Models;

/// <summary>
/// Represents a core workforce employee record in ERMS.
/// </summary>
public class Employee
{
    /// <summary>
    /// Unique identifier for the employee.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique organizational employee number/code (e.g., EMP-0001).
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Legal first name of the employee.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Middle name or initial (optional).
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Legal last name of the employee.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Work or primary contact email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact telephone or mobile number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Foreign key referencing the assigned department.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Job title or organizational position.
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Classification of employment (e.g., FullTime, PartTime, Probationary, Contractual).
    /// </summary>
    public string EmploymentType { get; set; } = string.Empty;

    /// <summary>
    /// Official date of hire in UTC.
    /// </summary>
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Current employment status (e.g., Active, OnLeave, Suspended, Terminated, Resigned).
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Physical residential or mailing address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Timestamp when record was created in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when record was last updated in UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Department to which this employee is assigned.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Associated system login account for this employee (if provisioned).
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Associated digital documents uploaded for this employee.
    /// </summary>
    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();

    /// <summary>
    /// Historical career and position change records.
    /// </summary>
    public ICollection<EmploymentHistory> EmploymentHistories { get; set; } = new List<EmploymentHistory>();

    /// <summary>
    /// Submitted leave requests for this employee.
    /// </summary>
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
