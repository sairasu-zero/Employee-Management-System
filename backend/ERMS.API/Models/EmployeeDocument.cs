namespace ERMS.API.Models;

/// <summary>
/// Represents a secure digital document associated with an employee record.
/// </summary>
public class EmployeeDocument
{
    /// <summary>
    /// Unique identifier for the document record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key identifier referencing the associated Employee.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Display or descriptive name of the document.
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Document classification type (e.g., Resume, Contract, GovernmentId, Certificate, PolicyAgreement).
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Original file name as supplied by the client during upload.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Secure random server-generated file name on storage.
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type verified via signature (e.g., application/pdf, image/png, image/jpeg).
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes (max 10 MB).
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Full storage path or relative storage identifier on local disk/vault.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Identity or email of the user who performed the upload.
    /// </summary>
    public string? UploadedBy { get; set; }

    /// <summary>
    /// Timestamp when document was uploaded in UTC.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when document was last modified or verified in UTC.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete flag to preserve auditability and prevent hard record loss.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    // Backward-compatibility aliases
    public string Title
    {
        get => DocumentName;
        set => DocumentName = value;
    }

    public string Category
    {
        get => DocumentType;
        set => DocumentType = value;
    }

    public string FilePath
    {
        get => StoragePath;
        set => StoragePath = value;
    }

    // Navigation properties

    /// <summary>
    /// Employee entity to which this document belongs.
    /// </summary>
    public Employee Employee { get; set; } = null!;
}
