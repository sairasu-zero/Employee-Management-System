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
[Route("api")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDocumentStorageService _storageService;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        ApplicationDbContext dbContext,
        IDocumentStorageService storageService,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<DocumentsController> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    private async Task<User?> GetCurrentAuthUserAsync(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        if (Guid.TryParse(userIdStr, out var userId))
        {
            return await _dbContext.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _dbContext.Users
                .Include(u => u.Role)
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        return null;
    }

    private bool CheckAccessPermission(User user, Employee targetEmployee, bool writeOperation = false)
    {
        var role = user.Role?.Name ?? "Employee";

        if (role == "Admin" || role == "HR")
        {
            return true;
        }

        if (role == "Manager")
        {
            // Department managers can view/manage documents of employees in their department
            if (user.Employee?.DepartmentId != null &&
                user.Employee.DepartmentId == targetEmployee.DepartmentId)
            {
                return true;
            }
        }

        // Standard employees can view/upload to their own profile, but cannot delete documents
        if (user.EmployeeId.HasValue && user.EmployeeId.Value == targetEmployee.Id)
        {
            if (writeOperation && role == "Employee")
            {
                // Employees can upload to their profile
                return true;
            }
            return !writeOperation || role != "Employee";
        }

        return false;
    }

    /// <summary>
    /// Upload a new document for an employee.
    /// </summary>
    [HttpPost("employees/{id:guid}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EmployeeDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadDocument(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] string documentName,
        [FromForm] string documentType,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null) return Unauthorized();

        var employee = await _dbContext.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        if (!CheckAccessPermission(user, employee, writeOperation: true))
        {
            return Forbid();
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        using var stream = file.OpenReadStream();
        var (isValid, errorMessage, detectedContentType) = await _storageService.ValidateFileAsync(
            stream, file.FileName, file.ContentType, cancellationToken);

        if (!isValid)
        {
            return BadRequest(errorMessage ?? "File validation failed.");
        }

        var storeResult = await _storageService.SaveFileAsync(
            stream, file.FileName, detectedContentType!, cancellationToken);

        var doc = new EmployeeDocument
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            DocumentName = string.IsNullOrWhiteSpace(documentName) ? Path.GetFileNameWithoutExtension(file.FileName) : documentName.Trim(),
            DocumentType = string.IsNullOrWhiteSpace(documentType) ? "General" : documentType.Trim(),
            OriginalFileName = file.FileName,
            StoredFileName = storeResult.StoredFileName,
            ContentType = storeResult.DetectedContentType,
            FileSize = storeResult.FileSize,
            StoragePath = storeResult.StoragePath,
            UploadedBy = user.Employee != null ? $"{user.Employee.FirstName} {user.Employee.LastName}".Trim() : user.Username,
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _dbContext.EmployeeDocuments.AddAsync(doc, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "UploadDocument", "DocumentManagement",
            $"Uploaded document '{doc.DocumentName}' ({doc.OriginalFileName}, {doc.FileSize / 1024.0:F1} KB) for employee {employee.EmployeeNumber}",
            $"EmployeeDocument/{doc.Id}", "Success", ip, cancellationToken);

        await _notificationService.SendToRolesAsync(new[] { "HR", "Admin" }, new NotificationMessage(
            Guid.NewGuid().ToString(),
            "Employee Document Uploaded",
            $"New document '{doc.DocumentName}' was uploaded for {employee.FirstName} {employee.LastName}.",
            "Document",
            "info",
            DateTime.UtcNow,
            $"/employees/{employee.Id}"
        ), cancellationToken);

        var dto = new EmployeeDocumentDto(
            doc.Id,
            doc.EmployeeId,
            doc.DocumentName,
            doc.DocumentType,
            doc.OriginalFileName,
            doc.ContentType,
            doc.FileSize,
            doc.UploadedBy,
            doc.UploadedAt,
            doc.UpdatedAt
        );

        return CreatedAtAction(nameof(DownloadDocument), new { id = doc.Id }, dto);
    }

    /// <summary>
    /// Retrieve documents associated with an employee.
    /// </summary>
    [HttpGet("employees/{id:guid}/documents")]
    [ProducesResponseType(typeof(List<EmployeeDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeDocuments(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null) return Unauthorized();

        var employee = await _dbContext.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee == null) return NotFound("Employee not found.");

        if (!CheckAccessPermission(user, employee, writeOperation: false))
        {
            return Forbid();
        }

        var docs = await _dbContext.EmployeeDocuments
            .Where(d => d.EmployeeId == id && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new EmployeeDocumentDto(
                d.Id,
                d.EmployeeId,
                d.DocumentName,
                d.DocumentType,
                d.OriginalFileName,
                d.ContentType,
                d.FileSize,
                d.UploadedBy,
                d.UploadedAt,
                d.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(docs);
    }

    /// <summary>
    /// Download a document after server-side authorization.
    /// </summary>
    [HttpGet("documents/{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null) return Unauthorized();

        var doc = await _dbContext.EmployeeDocuments
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (doc == null)
        {
            return NotFound("Document not found.");
        }

        if (!CheckAccessPermission(user, doc.Employee, writeOperation: false))
        {
            var ipDeny = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogActivityAsync(
                user.Id, user.Email, "DownloadDocument", "DocumentManagement",
                $"Unauthorized download attempt for document '{doc.DocumentName}'",
                $"EmployeeDocument/{doc.Id}", "Denied", ipDeny, cancellationToken);
            return Forbid();
        }

        var (stream, contentType) = await _storageService.GetFileStreamAsync(
            doc.StoragePath, doc.ContentType, cancellationToken);

        if (stream == null)
        {
            return NotFound("Stored file binary could not be found on storage.");
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "DownloadDocument", "DocumentManagement",
            $"Downloaded document '{doc.DocumentName}' ({doc.OriginalFileName})",
            $"EmployeeDocument/{doc.Id}", "Success", ip, cancellationToken);

        return File(stream, contentType, doc.OriginalFileName);
    }

    /// <summary>
    /// Soft-delete an employee document when authorized.
    /// </summary>
    [HttpDelete("documents/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetCurrentAuthUserAsync(cancellationToken);
        if (user == null) return Unauthorized();

        var doc = await _dbContext.EmployeeDocuments
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, cancellationToken);

        if (doc == null)
        {
            return NotFound("Document not found.");
        }

        var role = user.Role?.Name ?? "Employee";
        if (role != "Admin" && role != "HR")
        {
            return Forbid();
        }

        doc.IsDeleted = true;
        doc.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogActivityAsync(
            user.Id, user.Email, "DeleteDocument", "DocumentManagement",
            $"Soft-deleted document '{doc.DocumentName}' for employee {doc.Employee.EmployeeNumber}",
            $"EmployeeDocument/{doc.Id}", "Success", ip, cancellationToken);

        return NoContent();
    }
}
