using ERMS.API.Models;

namespace ERMS.API.Interfaces;

/// <summary>
/// Service interface governing system security audit logging, compliance tracking, and login attempt logging.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records a system activity or administrative action in the immutable audit log trail.
    /// </summary>
    /// <param name="userId">Unique identifier of user executing action, if authenticated.</param>
    /// <param name="userIdentity">Display name or email of user or actor.</param>
    /// <param name="activityType">Action verb or code (e.g., "Login", "CreateEmployee", "RevokeRole").</param>
    /// <param name="category">Functional category (e.g., "Authentication", "EmployeeManagement", "Security").</param>
    /// <param name="description">Comprehensive description of the event or data changes.</param>
    /// <param name="targetResource">Identifier or description of modified target resource.</param>
    /// <param name="actionResult">Outcome status (e.g., "Success", "Failed", "Denied").</param>
    /// <param name="ipAddress">Originating IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogActivityAsync(
        Guid? userId,
        string? userIdentity,
        string activityType,
        string category,
        string description,
        string? targetResource = null,
        string actionResult = "Success",
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an authentication attempt for security monitoring and brute-force detection.
    /// </summary>
    /// <param name="usernameOrEmail">Username or email attempted.</param>
    /// <param name="isSuccessful">Whether authentication succeeded.</param>
    /// <param name="failureReason">Failure explanation if failed.</param>
    /// <param name="ipAddress">Originating IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogLoginAttemptAsync(
        string usernameOrEmail,
        bool isSuccessful,
        string? failureReason = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of audit logs with optional filtering.
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        string? category = null,
        string? activityType = null,
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}
