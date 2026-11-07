namespace ERMS.API.Models;

/// <summary>
/// Represents an employee's formal leave request and approval workflow lifecycle.
/// </summary>
public class LeaveRequest
{
    /// <summary>
    /// Unique identifier for the leave request.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key referencing the Employee submitting the request.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Category of leave (e.g., Annual, Sick, Personal, Maternity, Paternity, Bereavement, Unpaid).
    /// </summary>
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>
    /// Start date of requested leave in UTC.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of requested leave in UTC.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total duration in business/calendar days.
    /// </summary>
    public int Days { get; set; }

    /// <summary>
    /// Justification or reason provided by the employee.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Status: Pending, Approved, Rejected, Cancelled.
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Identifier or name of the approving authority (Manager or HR Officer).
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Timestamp when request was approved or rejected in UTC.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Reason provided if the request was rejected.
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Timestamp when request was submitted in UTC.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when request was last updated in UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Employee requesting the leave.
    /// </summary>
    public Employee Employee { get; set; } = null!;
}
