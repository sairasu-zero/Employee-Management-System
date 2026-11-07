using System.Security.Claims;
using ERMS.API.Data;
using ERMS.API.DTOs;
using ERMS.API.Interfaces;
using ERMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Controllers;

[ApiController]
[Route("api/leaves")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(
        ApplicationDbContext dbContext,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<LeaveRequestsController> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Helper to retrieve the current authenticated User with Role and Employee details.
    /// </summary>
    private async Task<User?> GetCurrentAuthUserAsync(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (Guid.TryParse(userIdStr, out var userId))
        {
            return await _dbContext.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _dbContext.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Submit a new leave request for the authenticated employee.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitLeaveRequest(
        [FromBody] CreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized("User identity could not be verified.");
        }

        if (!user.EmployeeId.HasValue || user.Employee == null)
        {
            return BadRequest("Only users associated with an employee profile can submit leave requests.");
        }

        if (string.IsNullOrWhiteSpace(request.LeaveType) || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest("Leave type and reason are required.");
        }

        var startDateUtc = request.StartDate.ToUniversalTime().Date;
        var endDateUtc = request.EndDate.ToUniversalTime().Date;

        if (endDateUtc < startDateUtc)
        {
            return BadRequest("End date cannot precede start date.");
        }

        int calculatedDays = (endDateUtc - startDateUtc).Days + 1;
        if (calculatedDays < 1)
        {
            return BadRequest("Leave duration must be at least 1 day.");
        }

        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = user.EmployeeId.Value,
            LeaveType = request.LeaveType.Trim(),
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Days = calculatedDays,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.LeaveRequests.AddAsync(leave, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "SubmitLeaveRequest", "LeaveManagement",
            $"Submitted {leave.LeaveType} leave request for {leave.Days} day(s) from {leave.StartDate:yyyy-MM-dd} to {leave.EndDate:yyyy-MM-dd}",
            $"LeaveRequest/{leave.Id}", "Success", ip, cancellationToken);

        var emp = user.Employee;

        await _notificationService.SendToRolesAsync(new[] { "Manager", "HR", "Admin" }, new NotificationMessage(
            Guid.NewGuid().ToString(),
            "New Leave Request Submitted",
            $"{emp.FirstName} {emp.LastName} requested {leave.Days} day(s) of {leave.LeaveType} leave.",
            "Leave",
            "info",
            DateTime.UtcNow,
            "/manager"
        ), cancellationToken);
        var dto = new LeaveRequestDto(
            leave.Id,
            leave.EmployeeId,
            emp.EmployeeNumber,
            $"{emp.FirstName} {emp.LastName}".Trim(),
            emp.DepartmentId,
            emp.Department?.Name,
            leave.LeaveType,
            leave.StartDate,
            leave.EndDate,
            leave.Days,
            leave.Reason,
            leave.Status,
            leave.ApprovedBy,
            leave.ApprovedAt,
            leave.RejectionReason,
            leave.CreatedAt,
            leave.UpdatedAt
        );

        return CreatedAtAction(nameof(GetLeaveById), new { id = leave.Id }, dto);
    }

    /// <summary>
    /// Retrieve leave requests according to the authenticated user's permissions and organizational scope.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] string? status = null,
        [FromQuery] Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        var roleName = user.Role?.Name ?? "Employee";
        var query = _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .AsNoTracking();

        if (roleName == "Admin" || roleName == "HR")
        {
            // Full organizational visibility
            if (departmentId.HasValue)
            {
                query = query.Where(lr => lr.Employee.DepartmentId == departmentId.Value);
            }
        }
        else if (roleName == "Manager")
        {
            // Department managers can see requests from their department and their own requests
            var managerDeptId = user.Employee?.DepartmentId;
            query = query.Where(lr =>
                (managerDeptId.HasValue && lr.Employee.DepartmentId == managerDeptId.Value) ||
                (user.EmployeeId.HasValue && lr.EmployeeId == user.EmployeeId.Value));
        }
        else
        {
            // Standard employees can only see their own requests
            if (!user.EmployeeId.HasValue)
            {
                return Ok(new List<LeaveRequestDto>());
            }
            query = query.Where(lr => lr.EmployeeId == user.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(lr => lr.Status.ToLower() == status.Trim().ToLower());
        }

        var list = await query
            .OrderByDescending(lr => lr.CreatedAt)
            .Select(lr => new LeaveRequestDto(
                lr.Id,
                lr.EmployeeId,
                lr.Employee.EmployeeNumber,
                $"{lr.Employee.FirstName} {lr.Employee.LastName}".Trim(),
                lr.Employee.DepartmentId,
                lr.Employee.Department != null ? lr.Employee.Department.Name : null,
                lr.LeaveType,
                lr.StartDate,
                lr.EndDate,
                lr.Days,
                lr.Reason,
                lr.Status,
                lr.ApprovedBy,
                lr.ApprovedAt,
                lr.RejectionReason,
                lr.CreatedAt,
                lr.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(list);
    }

    /// <summary>
    /// Retrieve pending leave requests for authorized managers and HR officers within permitted organizational scope.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingLeaveRequests(CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        var roleName = user.Role?.Name ?? "Employee";
        if (roleName != "Admin" && roleName != "HR" && roleName != "Manager")
        {
            return Forbid();
        }

        var query = _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .Where(lr => lr.Status == "Pending")
            .AsNoTracking();

        if (roleName == "Manager")
        {
            var managerDeptId = user.Employee?.DepartmentId;
            if (!managerDeptId.HasValue)
            {
                return Ok(new List<LeaveRequestDto>());
            }

            // Managers review their department team members, excluding self
            query = query.Where(lr =>
                lr.Employee.DepartmentId == managerDeptId.Value &&
                (!user.EmployeeId.HasValue || lr.EmployeeId != user.EmployeeId.Value));
        }

        var pendingList = await query
            .OrderBy(lr => lr.CreatedAt)
            .Select(lr => new LeaveRequestDto(
                lr.Id,
                lr.EmployeeId,
                lr.Employee.EmployeeNumber,
                $"{lr.Employee.FirstName} {lr.Employee.LastName}".Trim(),
                lr.Employee.DepartmentId,
                lr.Employee.Department != null ? lr.Employee.Department.Name : null,
                lr.LeaveType,
                lr.StartDate,
                lr.EndDate,
                lr.Days,
                lr.Reason,
                lr.Status,
                lr.ApprovedBy,
                lr.ApprovedAt,
                lr.RejectionReason,
                lr.CreatedAt,
                lr.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(pendingList);
    }

    /// <summary>
    /// Retrieve an individual leave request by identifier with authorization checks.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveById(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        var lr = await _dbContext.LeaveRequests
            .Include(l => l.Employee)
            .ThenInclude(e => e.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (lr == null)
        {
            return NotFound("Leave request not found.");
        }

        var roleName = user.Role?.Name ?? "Employee";

        // Authorization validation
        if (roleName != "Admin" && roleName != "HR")
        {
            if (roleName == "Manager")
            {
                bool isDeptMember = user.Employee?.DepartmentId != null &&
                                   user.Employee.DepartmentId == lr.Employee.DepartmentId;
                bool isSelf = user.EmployeeId.HasValue && user.EmployeeId.Value == lr.EmployeeId;

                if (!isDeptMember && !isSelf)
                {
                    return Forbid();
                }
            }
            else
            {
                if (!user.EmployeeId.HasValue || user.EmployeeId.Value != lr.EmployeeId)
                {
                    return Forbid();
                }
            }
        }

        return Ok(new LeaveRequestDto(
            lr.Id,
            lr.EmployeeId,
            lr.Employee.EmployeeNumber,
            $"{lr.Employee.FirstName} {lr.Employee.LastName}".Trim(),
            lr.Employee.DepartmentId,
            lr.Employee.Department?.Name,
            lr.LeaveType,
            lr.StartDate,
            lr.EndDate,
            lr.Days,
            lr.Reason,
            lr.Status,
            lr.ApprovedBy,
            lr.ApprovedAt,
            lr.RejectionReason,
            lr.CreatedAt,
            lr.UpdatedAt
        ));
    }

    /// <summary>
    /// Approve a pending leave request.
    /// </summary>
    [HttpPut("{id:guid}/approve")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        var roleName = user.Role?.Name ?? "Employee";
        if (roleName != "Admin" && roleName != "HR" && roleName != "Manager")
        {
            return Forbid();
        }

        var leave = await _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (leave == null)
        {
            return NotFound("Leave request not found.");
        }

        if (leave.Status != "Pending")
        {
            return BadRequest($"Cannot approve a request with status '{leave.Status}'. Only pending requests can be approved.");
        }

        // Business rule: Employees cannot approve their own requests
        if (user.EmployeeId.HasValue && user.EmployeeId.Value == leave.EmployeeId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Employees cannot approve their own leave requests.");
        }

        // Business rule: Managers can only approve requests within their authorized department
        if (roleName == "Manager")
        {
            if (!user.Employee?.DepartmentId.HasValue == true ||
                user.Employee.DepartmentId != leave.Employee.DepartmentId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Managers can only approve requests within their authorized department.");
            }
        }

        var approverName = user.Employee != null
            ? $"{user.Employee.FirstName} {user.Employee.LastName}".Trim()
            : user.Username;

        leave.Status = "Approved";
        leave.ApprovedBy = approverName;
        leave.ApprovedAt = DateTime.UtcNow;
        leave.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var empInfo = leave.Employee != null ? $"{leave.Employee.FirstName} {leave.Employee.LastName} ({leave.Employee.EmployeeNumber})" : leave.EmployeeId.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "ApproveLeaveRequest", "LeaveManagement",
            $"Approved {leave.LeaveType} leave request for {empInfo}",
            $"LeaveRequest/{leave.Id}", "Success", ip, cancellationToken);

        await _notificationService.SendToRolesAsync(new[] { "HR", "Admin" }, new NotificationMessage(
            Guid.NewGuid().ToString(),
            "Leave Request Approved",
            $"{leave.LeaveType} leave for {empInfo} was approved by {approverName}.",
            "Leave",
            "success",
            DateTime.UtcNow,
            "/hr"
        ), cancellationToken);

        return Ok(new LeaveRequestDto(
            leave.Id,
            leave.EmployeeId,
            leave.Employee.EmployeeNumber,
            $"{leave.Employee.FirstName} {leave.Employee.LastName}".Trim(),
            leave.Employee.DepartmentId,
            leave.Employee.Department?.Name,
            leave.LeaveType,
            leave.StartDate,
            leave.EndDate,
            leave.Days,
            leave.Reason,
            leave.Status,
            leave.ApprovedBy,
            leave.ApprovedAt,
            leave.RejectionReason,
            leave.CreatedAt,
            leave.UpdatedAt
        ));
    }

    /// <summary>
    /// Reject a pending leave request with an explanation.
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectLeaveRequest(
        Guid id,
        [FromBody] RejectLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest("A reason must be provided when rejecting a leave request.");
        }

        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        var roleName = user.Role?.Name ?? "Employee";
        if (roleName != "Admin" && roleName != "HR" && roleName != "Manager")
        {
            return Forbid();
        }

        var leave = await _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (leave == null)
        {
            return NotFound("Leave request not found.");
        }

        if (leave.Status != "Pending")
        {
            return BadRequest($"Cannot reject a request with status '{leave.Status}'. Only pending requests can be rejected.");
        }

        // Business rule: Employees cannot reject their own requests
        if (user.EmployeeId.HasValue && user.EmployeeId.Value == leave.EmployeeId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Employees cannot reject their own leave requests.");
        }

        // Business rule: Managers can only reject requests within their authorized department
        if (roleName == "Manager")
        {
            if (!user.Employee?.DepartmentId.HasValue == true ||
                user.Employee.DepartmentId != leave.Employee.DepartmentId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Managers can only reject requests within their authorized department.");
            }
        }

        var approverName = user.Employee != null
            ? $"{user.Employee.FirstName} {user.Employee.LastName}".Trim()
            : user.Username;

        leave.Status = "Rejected";
        leave.RejectionReason = request.Reason.Trim();
        leave.ApprovedBy = approverName;
        leave.ApprovedAt = DateTime.UtcNow;
        leave.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var empInfo = leave.Employee != null ? $"{leave.Employee.FirstName} {leave.Employee.LastName}" : leave.EmployeeId.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "RejectLeaveRequest", "LeaveManagement",
            $"Rejected {leave.LeaveType} leave request for {empInfo}. Reason: {leave.RejectionReason}",
            $"LeaveRequest/{leave.Id}", "Success", ip, cancellationToken);

        await _notificationService.SendToRolesAsync(new[] { "HR", "Admin" }, new NotificationMessage(
            Guid.NewGuid().ToString(),
            "Leave Request Rejected",
            $"{leave.LeaveType} leave for {empInfo} was declined: {leave.RejectionReason}",
            "Leave",
            "danger",
            DateTime.UtcNow,
            "/hr"
        ), cancellationToken);

        return Ok(new LeaveRequestDto(
            leave.Id,
            leave.EmployeeId,
            leave.Employee.EmployeeNumber,
            $"{leave.Employee.FirstName} {leave.Employee.LastName}".Trim(),
            leave.Employee.DepartmentId,
            leave.Employee.Department?.Name,
            leave.LeaveType,
            leave.StartDate,
            leave.EndDate,
            leave.Days,
            leave.Reason,
            leave.Status,
            leave.ApprovedBy,
            leave.ApprovedAt,
            leave.RejectionReason,
            leave.CreatedAt,
            leave.UpdatedAt
        ));
    }

    /// <summary>
    /// Allow employees to cancel their own eligible pending leave requests.
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        var leave = await _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
            .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (leave == null)
        {
            return NotFound("Leave request not found.");
        }

        // Only the employee who submitted the request (or Admin) can cancel it
        var roleName = user.Role?.Name ?? "Employee";
        bool isOwner = user.EmployeeId.HasValue && user.EmployeeId.Value == leave.EmployeeId;

        if (!isOwner && roleName != "Admin")
        {
            return StatusCode(StatusCodes.Status403Forbidden, "You can only cancel your own leave requests.");
        }

        if (leave.Status != "Pending")
        {
            return BadRequest($"Cannot cancel a request with status '{leave.Status}'. Only pending requests can be cancelled.");
        }

        leave.Status = "Cancelled";
        leave.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "CancelLeaveRequest", "LeaveManagement",
            $"Cancelled {leave.LeaveType} leave request for {leave.Employee.FirstName} {leave.Employee.LastName}",
            $"LeaveRequest/{leave.Id}", "Success", ip, cancellationToken);

        return Ok(new LeaveRequestDto(
            leave.Id,
            leave.EmployeeId,
            leave.Employee.EmployeeNumber,
            $"{leave.Employee.FirstName} {leave.Employee.LastName}".Trim(),
            leave.Employee.DepartmentId,
            leave.Employee.Department?.Name,
            leave.LeaveType,
            leave.StartDate,
            leave.EndDate,
            leave.Days,
            leave.Reason,
            leave.Status,
            leave.ApprovedBy,
            leave.ApprovedAt,
            leave.RejectionReason,
            leave.CreatedAt,
            leave.UpdatedAt
        ));
    }
}
