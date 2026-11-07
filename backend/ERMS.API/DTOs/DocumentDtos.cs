namespace ERMS.API.DTOs;

public record EmployeeDocumentDto(
    Guid Id,
    Guid EmployeeId,
    string DocumentName,
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string? UploadedBy,
    DateTime UploadedAt,
    DateTime? UpdatedAt
);

public record UploadDocumentRequestDto(
    string DocumentName,
    string DocumentType
);
