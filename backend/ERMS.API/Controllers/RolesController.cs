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
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        ApplicationDbContext dbContext,
        IAuditService auditService,
        ILogger<RolesController> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    private static readonly string[] StandardPermissions = new[]
    {
        "employees:read", "employees:create", "employees:write", "employees:delete",
        "departments:read", "departments:manage",
        "documents:read", "documents:upload", "documents:download",
        "leaves:read", "leaves:create", "leaves:approve", "leaves:reject",
        "audit:read", "users:manage", "roles:manage", "security:manage"
    };

    private async Task EnsureAllStandardPermissionsExistAsync(CancellationToken cancellationToken)
    {
        var existingPerms = await _dbContext.Permissions.Select(p => p.Name).ToListAsync(cancellationToken);
        var missing = StandardPermissions.Where(p => !existingPerms.Contains(p)).ToList();

        if (missing.Any())
        {
            foreach (var permName in missing)
            {
                var parts = permName.Split(':');
                var module = parts.Length > 0 ? char.ToUpper(parts[0][0]) + parts[0][1..] : "General";
                var action = parts.Length > 1 ? char.ToUpper(parts[1][0]) + parts[1][1..] : "Access";

                await _dbContext.Permissions.AddAsync(new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = permName,
                    Module = module,
                    Action = action,
                    Description = $"Allows {action.ToLower()} access to {module.ToLower()} resources."
                }, cancellationToken);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Retrieve all available organizational roles and their assigned permissions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        await EnsureAllStandardPermissionsExistAsync(cancellationToken);

        var roles = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsSystemRole,
                r.CreatedAt,
                r.RolePermissions
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new PermissionDto(
                        rp.Permission!.Id,
                        rp.Permission.Module,
                        rp.Permission.Action,
                        rp.Permission.Name,
                        rp.Permission.Description
                    ))
                    .OrderBy(p => p.Name)
                    .ToList()
            ))
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    /// <summary>
    /// Retrieve all cataloged permissions in the system.
    /// </summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken)
    {
        await EnsureAllStandardPermissionsExistAsync(cancellationToken);

        var permissions = await _dbContext.Permissions
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .Select(p => new PermissionDto(
                p.Id,
                p.Module,
                p.Action,
                p.Name,
                p.Description
            ))
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }

    /// <summary>
    /// Retrieve details of a specific role and its assigned permissions.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role == null)
        {
            return NotFound("Role not found.");
        }

        var dto = new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.CreatedAt,
            role.RolePermissions
                .Where(rp => rp.Permission != null)
                .Select(rp => new PermissionDto(
                    rp.Permission!.Id,
                    rp.Permission.Module,
                    rp.Permission.Action,
                    rp.Permission.Name,
                    rp.Permission.Description
                ))
                .OrderBy(p => p.Name)
                .ToList()
        );

        return Ok(dto);
    }

    /// <summary>
    /// Create a custom organizational role (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Role name is required.");
        }

        var normalizedName = request.Name.Trim();
        bool exists = await _dbContext.Roles.AnyAsync(r => r.Name.ToLower() == normalizedName.ToLower(), cancellationToken);
        if (exists)
        {
            return BadRequest($"Role '{normalizedName}' already exists.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Description = request.Description?.Trim(),
            IsSystemRole = false,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Roles.AddAsync(role, cancellationToken);

        if (request.PermissionNames != null && request.PermissionNames.Any())
        {
            var requestedPerms = await _dbContext.Permissions
                .Where(p => request.PermissionNames.Contains(p.Name))
                .ToListAsync(cancellationToken);

            foreach (var p in requestedPerms)
            {
                await _dbContext.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Admin"
                }, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
        await _auditService.LogActivityAsync(
            null, userEmail, "CreateRole", "RoleManagement",
            $"Created custom role '{role.Name}'",
            $"Role/{role.Id}", "Success", ip, cancellationToken);

        return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.CreatedAt,
            new List<PermissionDto>()
        ));
    }

    /// <summary>
    /// Update role details (Admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role == null)
        {
            return NotFound("Role not found.");
        }

        if (role.IsSystemRole && !string.Equals(role.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Core system roles (Admin, HR, Manager, Employee) cannot be renamed.");
        }

        role.Description = request.Description?.Trim();
        if (!role.IsSystemRole)
        {
            role.Name = request.Name.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
        await _auditService.LogActivityAsync(
            null, userEmail, "UpdateRole", "RoleManagement",
            $"Updated role details for '{role.Name}'",
            $"Role/{role.Id}", "Success", ip, cancellationToken);

        return Ok(new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.CreatedAt,
            role.RolePermissions
                .Where(rp => rp.Permission != null)
                .Select(rp => new PermissionDto(
                    rp.Permission!.Id,
                    rp.Permission.Module,
                    rp.Permission.Action,
                    rp.Permission.Name,
                    rp.Permission.Description
                ))
                .ToList()
        ));
    }

    /// <summary>
    /// Assign or revoke permissions for a role (Admin only).
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequestDto request,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role == null)
        {
            return NotFound("Role not found.");
        }

        // Anti-privilege escalation guard:
        // If modifying Admin role, preserve roles:manage and security:manage
        if (role.Name == "Admin")
        {
            if (!request.PermissionNames.Contains("roles:manage"))
            {
                request.PermissionNames.Add("roles:manage");
            }
            if (!request.PermissionNames.Contains("security:manage"))
            {
                request.PermissionNames.Add("security:manage");
            }
        }

        // Remove existing permissions
        _dbContext.RolePermissions.RemoveRange(role.RolePermissions);

        // Fetch valid requested permissions from database
        var validPermissions = await _dbContext.Permissions
            .Where(p => request.PermissionNames.Contains(p.Name))
            .ToListAsync(cancellationToken);

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";
        foreach (var p in validPermissions)
        {
            await _dbContext.RolePermissions.AddAsync(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = p.Id,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = userEmail
            }, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            null, userEmail, "UpdateRolePermissions", "RoleManagement",
            $"Updated permissions for role '{role.Name}'. Assigned {validPermissions.Count} permission(s).",
            $"Role/{role.Id}", "Success", ip, cancellationToken);

        return await GetRoleById(role.Id, cancellationToken);
    }
}
