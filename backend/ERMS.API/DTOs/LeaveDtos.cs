namespace ERMS.API.DTOs;

public record CreateLeaveRequestDto(
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    string Reason
);

public record RejectLeaveRequestDto(
    string Reason
);

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeName,
    Guid? DepartmentId,
    string? DepartmentName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate,
    int Days,
    string Reason,
    string Status,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
