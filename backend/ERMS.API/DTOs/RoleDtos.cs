namespace ERMS.API.DTOs;

public record PermissionDto(
    Guid Id,
    string Module,
    string Action,
    string Name,
    string? Description
);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    DateTime CreatedAt,
    List<PermissionDto> Permissions
);

public record CreateRoleRequestDto(
    string Name,
    string? Description,
    List<string>? PermissionNames
);

public record UpdateRoleRequestDto(
    string Name,
    string? Description
);

public record UpdateRolePermissionsRequestDto(
    List<string> PermissionNames
);
