using System.Security.Claims;
using ERMS.API.Data;
using ERMS.API.DTOs;
using ERMS.API.Interfaces;
using ERMS.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        ApplicationDbContext dbContext,
        IAuditService auditService,
        ILogger<EmployeesController> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Employees
            .Include(e => e.Department)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(s) ||
                e.LastName.ToLower().Contains(s) ||
                e.Email.ToLower().Contains(s) ||
                e.EmployeeNumber.ToLower().Contains(s) ||
                e.Position.ToLower().Contains(s));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status.ToLower() == status.Trim().ToLower());
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var employees = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeDto(
                e.Id,
                e.EmployeeNumber,
                e.FirstName,
                e.MiddleName,
                e.LastName,
                $"{e.FirstName} {e.LastName}".Trim(),
                e.Email,
                e.Phone,
                e.DepartmentId,
                e.Department != null ? e.Department.Name : null,
                e.Position,
                e.EmploymentType,
                e.HireDate,
                e.Status,
                e.Address,
                e.CreatedAt,
                e.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResultDto<EmployeeDto>(employees, totalCount, page, pageSize, totalPages));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeById(Guid id, CancellationToken cancellationToken)
    {
        var e = await _dbContext.Employees
            .Include(emp => emp.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(emp => emp.Id == id, cancellationToken);

        if (e == null)
        {
            return NotFound("Employee not found.");
        }

        return Ok(new EmployeeDto(
            e.Id,
            e.EmployeeNumber,
            e.FirstName,
            e.MiddleName,
            e.LastName,
            $"{e.FirstName} {e.LastName}".Trim(),
            e.Email,
            e.Phone,
            e.DepartmentId,
            e.Department != null ? e.Department.Name : null,
            e.Position,
            e.EmploymentType,
            e.HireDate,
            e.Status,
            e.Address,
            e.CreatedAt,
            e.UpdatedAt
        ));
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("First Name, Last Name, and Email are required.");
        }

        string employeeNumber = $"EMP-{Random.Shared.Next(1000, 9999)}";

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone?.Trim(),
            DepartmentId = request.DepartmentId,
            Position = request.Position.Trim(),
            EmploymentType = request.EmploymentType,
            HireDate = request.HireDate.ToUniversalTime(),
            Status = request.Status,
            Address = request.Address?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Employees.AddAsync(employee, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        await _auditService.LogActivityAsync(
            null, actorEmail, "CreateEmployee", "EmployeeManagement",
            $"Created employee record {employee.EmployeeNumber} ({employee.FirstName} {employee.LastName})",
            $"Employee/{employee.Id}", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, new EmployeeDto(
            employee.Id,
            employee.EmployeeNumber,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            $"{employee.FirstName} {employee.LastName}".Trim(),
            employee.Email,
            employee.Phone,
            employee.DepartmentId,
            null,
            employee.Position,
            employee.EmploymentType,
            employee.HireDate,
            employee.Status,
            employee.Address,
            employee.CreatedAt,
            employee.UpdatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null)
        {
            return NotFound("Employee not found.");
        }

        // Track employment history change if position or department changed
        if (employee.Position != request.Position || employee.DepartmentId != request.DepartmentId)
        {
            _dbContext.EmploymentHistories.Add(new EmploymentHistory
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                PreviousPosition = employee.Position,
                NewPosition = request.Position,
                PreviousDepartmentId = employee.DepartmentId,
                NewDepartmentId = request.DepartmentId,
                ChangeType = employee.Position != request.Position ? "Promotion/TitleChange" : "DepartmentTransfer",
                ChangeDate = DateTime.UtcNow,
                Remarks = "Updated via ERMS workforce portal"
            });
        }

        employee.FirstName = request.FirstName.Trim();
        employee.MiddleName = request.MiddleName?.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Email = request.Email.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.DepartmentId = request.DepartmentId;
        employee.Position = request.Position.Trim();
        employee.EmploymentType = request.EmploymentType;
        employee.HireDate = request.HireDate.ToUniversalTime();
        employee.Status = request.Status;
        employee.Address = request.Address?.Trim();
        employee.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        await _auditService.LogActivityAsync(
            null, actorEmail, "UpdateEmployee", "EmployeeManagement",
            $"Updated employee record {employee.EmployeeNumber}",
            $"Employee/{employee.Id}", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return Ok(new EmployeeDto(
            employee.Id,
            employee.EmployeeNumber,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            $"{employee.FirstName} {employee.LastName}".Trim(),
            employee.Email,
            employee.Phone,
            employee.DepartmentId,
            null,
            employee.Position,
            employee.EmploymentType,
            employee.HireDate,
            employee.Status,
            employee.Address,
            employee.CreatedAt,
            employee.UpdatedAt
        ));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null)
        {
            return NotFound("Employee not found.");
        }

        employee.Status = "Terminated";
        employee.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        await _auditService.LogActivityAsync(
            null, actorEmail, "DeactivateEmployee", "EmployeeManagement",
            $"Deactivated employee record {employee.EmployeeNumber}",
            $"Employee/{employee.Id}", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return NoContent();
    }
}
