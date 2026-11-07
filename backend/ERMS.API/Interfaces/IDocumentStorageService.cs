namespace ERMS.API.Interfaces;

public record StoredFileResult(
    string StoredFileName,
    string StoragePath,
    string DetectedContentType,
    long FileSize
);

public interface IDocumentStorageService
{
    /// <summary>
    /// Validates file size, extension, and binary signature magic bytes.
    /// Supports PDF, PNG, and JPEG up to 10 MB.
    /// </summary>
    Task<(bool IsValid, string? ErrorMessage, string? DetectedContentType)> ValidateFileAsync(
        Stream stream,
        string fileName,
        string clientContentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the validated file into secure local storage with random server-generated name
    /// and strict directory boundary validation (anti-path-traversal).
    /// </summary>
    Task<StoredFileResult> SaveFileAsync(
        Stream stream,
        string originalFileName,
        string detectedContentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file stream for a given stored relative path with directory traversal verification.
    /// </summary>
    Task<(Stream? FileStream, string ContentType)> GetFileStreamAsync(
        string storagePath,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely deletes the file from physical storage.
    /// </summary>
    Task<bool> DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}
