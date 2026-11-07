using ERMS.API.Data;
using ERMS.API.Interfaces;
using ERMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Services;

/// <summary>
/// Implements enterprise security audit logging and login attempt tracking.
/// Logs are committed asynchronously to PostgreSQL and mirrored to application logs.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext dbContext, ILogger<AuditService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records a system activity or administrative action in the immutable audit log trail.
    /// </summary>
    public async Task LogActivityAsync(
        Guid? userId,
        string? userIdentity,
        string activityType,
        string category,
        string description,
        string? targetResource = null,
        string actionResult = "Success",
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserIdentity = userIdentity,
                ActivityType = activityType,
                Category = category,
                Description = description,
                TargetResource = targetResource,
                ActionResult = actionResult,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit: [{Category}] {ActivityType} by '{User}' on '{Target}' - {Result}",
                category, activityType, userIdentity ?? "Anonymous", targetResource ?? "N/A", actionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist audit log entry for activity '{ActivityType}'", activityType);
        }
    }

    /// <summary>
    /// Records an authentication attempt for security monitoring and brute-force detection.
    /// </summary>
    public async Task LogLoginAttemptAsync(
        string usernameOrEmail,
        bool isSuccessful,
        string? failureReason = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attempt = new LoginAttempt
            {
                Id = Guid.NewGuid(),
                UsernameOrEmail = usernameOrEmail,
                IsSuccessful = isSuccessful,
                FailureReason = failureReason,
                IpAddress = ipAddress,
                AttemptedAt = DateTime.UtcNow
            };

            await _dbContext.LoginAttempts.AddAsync(attempt, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (isSuccessful)
            {
                _logger.LogInformation("Login attempt succeeded for user '{Username}' from IP {IP}", usernameOrEmail, ipAddress ?? "Unknown");
            }
            else
            {
                _logger.LogWarning("Login attempt failed for user '{Username}' from IP {IP}. Reason: {Reason}", usernameOrEmail, ipAddress ?? "Unknown", failureReason ?? "Unknown");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login attempt for username '{Username}'", usernameOrEmail);
        }
    }

    /// <summary>
    /// Retrieves a paginated list of audit logs with optional filtering.
    /// </summary>
    public async Task<(IReadOnlyList<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        string? category = null,
        string? activityType = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(activityType))
        {
            query = query.Where(a => a.ActivityType == activityType);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (logs, totalCount);
    }
}
