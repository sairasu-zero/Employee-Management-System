using ERMS.API.Data;
using ERMS.API.DTOs;
using ERMS.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;

    public AuditController(ApplicationDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? category = null,
        [FromQuery] string? activityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (logs, totalCount) = await _auditService.GetAuditLogsAsync(
            category, activityType, userId, startDate, endDate, page, pageSize, cancellationToken);

        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var dtoList = logs.Select(l => new AuditLogDto(
            l.Id,
            l.UserId,
            l.UserIdentity,
            l.ActivityType,
            l.Category,
            l.Description,
            l.TargetResource,
            l.ActionResult,
            l.IpAddress,
            l.Timestamp
        )).ToList();

        return Ok(new PagedResultDto<AuditLogDto>(dtoList, totalCount, page, pageSize, totalPages));
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(AuditDashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        int totalLogsToday = await _dbContext.AuditLogs
            .CountAsync(a => a.Timestamp >= today, cancellationToken);

        int failedLoginsToday = await _dbContext.LoginAttempts
            .CountAsync(l => !l.IsSuccessful && l.AttemptedAt >= today, cancellationToken);

        int securityAlerts = await _dbContext.AuditLogs
            .CountAsync(a => a.ActionResult == "Denied" || a.ActionResult == "Failed", cancellationToken);

        int totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        int mfaUsers = await _dbContext.Users
            .CountAsync(u => u.RequireMfa || (u.MfaConfiguration != null && u.MfaConfiguration.IsEnabled), cancellationToken);

        double mfaCompliance = totalUsers > 0 ? Math.Round((mfaUsers / (double)totalUsers) * 100, 1) : 100.0;

        int totalEmployees = await _dbContext.Employees.CountAsync(cancellationToken);
        int totalDepartments = await _dbContext.Departments.CountAsync(cancellationToken);

        return Ok(new AuditDashboardStatsDto(
            TotalAuditLogsToday: totalLogsToday,
            TotalSecurityAlerts: securityAlerts,
            FailedLoginsToday: failedLoginsToday,
            MfaComplianceRate: mfaCompliance,
            TotalEmployees: totalEmployees,
            TotalDepartments: totalDepartments
        ));
    }

    [HttpGet("export/csv")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? category = null,
        [FromQuery] string? activityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, _) = await _auditService.GetAuditLogsAsync(
            category, activityType, userId, startDate, endDate, 1, 10000, cancellationToken);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,TimestampUtc,ActorIdentity,Category,ActivityType,Description,TargetResource,ActionResult,IpAddress");

        foreach (var l in logs)
        {
            static string EscapeCsv(string? val)
            {
                if (string.IsNullOrEmpty(val)) return "\"\"";
                return $"\"{val.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ")}\"";
            }

            sb.AppendLine($"{l.Id},{l.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsv(l.UserIdentity)},{EscapeCsv(l.Category)},{EscapeCsv(l.ActivityType)},{EscapeCsv(l.Description)},{EscapeCsv(l.TargetResource)},{EscapeCsv(l.ActionResult)},{EscapeCsv(l.IpAddress)}");
        }

        var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Admin";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            null, actorEmail, "ExportAuditLogs", "AuditCompliance",
            $"Exported {logs.Count} audit records to CSV format.",
            "AuditLogs/Export", "Success", ip, cancellationToken);

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"audit_trail_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet("export/pdf")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string? category = null,
        [FromQuery] string? activityType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, _) = await _auditService.GetAuditLogsAsync(
            category, activityType, userId, startDate, endDate, 1, 1000, cancellationToken);

        var dtoList = logs.Select(l => new AuditLogDto(
            l.Id,
            l.UserId,
            l.UserIdentity,
            l.ActivityType,
            l.Category,
            l.Description,
            l.TargetResource,
            l.ActionResult,
            l.IpAddress,
            l.Timestamp
        )).ToList();

        var actorEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "Admin";
        var pdfBytes = Services.AuditPdfGenerator.GenerateAuditReportPdf(dtoList, actorEmail, DateTime.UtcNow);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            null, actorEmail, "ExportAuditLogs", "AuditCompliance",
            $"Exported {logs.Count} audit records to PDF report format.",
            "AuditLogs/Export", "Success", ip, cancellationToken);

        return File(pdfBytes, "application/pdf", $"audit_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }
}
