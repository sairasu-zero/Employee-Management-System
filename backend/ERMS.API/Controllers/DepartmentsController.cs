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
public class DepartmentsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        ApplicationDbContext dbContext,
        IAuditService auditService,
        ILogger<DepartmentsController> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        var departments = await _dbContext.Departments
            .Include(d => d.ManagerEmployee)
            .Include(d => d.Employees)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Name,
                d.Code,
                d.Description,
                d.ManagerEmployeeId,
                d.ManagerEmployee != null ? $"{d.ManagerEmployee.FirstName} {d.ManagerEmployee.LastName}".Trim() : null,
                d.Employees.Count(e => e.Status == "Active"),
                d.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(departments);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentById(Guid id, CancellationToken cancellationToken)
    {
        var d = await _dbContext.Departments
            .Include(dept => dept.ManagerEmployee)
            .Include(dept => dept.Employees)
            .AsNoTracking()
            .FirstOrDefaultAsync(dept => dept.Id == id, cancellationToken);

        if (d == null)
        {
            return NotFound("Department not found.");
        }

        return Ok(new DepartmentDto(
            d.Id,
            d.Name,
            d.Code,
            d.Description,
            d.ManagerEmployeeId,
            d.ManagerEmployee != null ? $"{d.ManagerEmployee.FirstName} {d.ManagerEmployee.LastName}".Trim() : null,
            d.Employees.Count(e => e.Status == "Active"),
            d.CreatedAt
        ));
    }

    [HttpPost]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest("Department Name and Code are required.");
        }

        bool codeExists = await _dbContext.Departments.AnyAsync(d => d.Code.ToUpper() == request.Code.ToUpper(), cancellationToken);
        if (codeExists)
        {
            return BadRequest($"A department with code '{request.Code}' already exists.");
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpper(),
            Description = request.Description?.Trim(),
            ManagerEmployeeId = request.ManagerEmployeeId,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Departments.AddAsync(department, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        await _auditService.LogActivityAsync(
            null, actorEmail, "CreateDepartment", "DepartmentManagement",
            $"Created department {department.Code} ({department.Name})",
            $"Department/{department.Id}", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return CreatedAtAction(nameof(GetDepartmentById), new { id = department.Id }, new DepartmentDto(
            department.Id,
            department.Name,
            department.Code,
            department.Description,
            department.ManagerEmployeeId,
            null,
            0,
            department.CreatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequestDto request, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments.FindAsync(new object[] { id }, cancellationToken);
        if (department == null)
        {
            return NotFound("Department not found.");
        }

        department.Name = request.Name.Trim();
        department.Code = request.Code.Trim().ToUpper();
        department.Description = request.Description?.Trim();
        department.ManagerEmployeeId = request.ManagerEmployeeId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var actorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        await _auditService.LogActivityAsync(
            null, actorEmail, "UpdateDepartment", "DepartmentManagement",
            $"Updated department {department.Code}",
            $"Department/{department.Id}", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return Ok(new DepartmentDto(
            department.Id,
            department.Name,
            department.Code,
            department.Description,
            department.ManagerEmployeeId,
            null,
            0,
            department.CreatedAt
        ));
    }
}
