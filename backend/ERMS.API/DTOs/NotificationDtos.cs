namespace ERMS.API.DTOs;

public record NotificationMessage(
    string Id,
    string Title,
    string Description,
    string Category, // "Leave", "Security", "Workforce", "Document", "Role"
    string Severity, // "info", "warning", "success", "danger"
    DateTime Timestamp,
    string? ActionUrl = null
);
