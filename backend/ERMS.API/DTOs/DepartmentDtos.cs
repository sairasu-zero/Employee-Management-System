namespace ERMS.API.DTOs;

public record DepartmentDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    Guid? ManagerEmployeeId,
    string? ManagerName,
    int Headcount,
    DateTime CreatedAt
);

public record CreateDepartmentRequestDto(
    string Name,
    string Code,
    string? Description = null,
    Guid? ManagerEmployeeId = null
);

public record UpdateDepartmentRequestDto(
    string Name,
    string Code,
    string? Description = null,
    Guid? ManagerEmployeeId = null
);
