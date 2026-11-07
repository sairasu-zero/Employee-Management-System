namespace ERMS.API.DTOs;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserIdentity,
    string ActivityType,
    string Category,
    string Description,
    string? TargetResource,
    string ActionResult,
    string? IpAddress,
    DateTime Timestamp
);

public record AuditDashboardStatsDto(
    int TotalAuditLogsToday,
    int TotalSecurityAlerts,
    int FailedLoginsToday,
    double MfaComplianceRate,
    int TotalEmployees,
    int TotalDepartments
);
