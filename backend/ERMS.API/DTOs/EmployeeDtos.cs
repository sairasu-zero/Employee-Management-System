namespace ERMS.API.DTOs;

public record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    Guid? DepartmentId,
    string? DepartmentName,
    string Position,
    string EmploymentType,
    DateTime HireDate,
    string Status,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateEmployeeRequestDto(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? Phone,
    Guid? DepartmentId,
    string Position,
    string EmploymentType,
    DateTime HireDate,
    string Status = "Active",
    string? Address = null
);

public record UpdateEmployeeRequestDto(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string? Phone,
    Guid? DepartmentId,
    string Position,
    string EmploymentType,
    DateTime HireDate,
    string Status,
    string? Address = null
);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
