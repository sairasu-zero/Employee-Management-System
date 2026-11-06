using System.Text.Json.Serialization;

namespace ERMS.API.DTOs;

/// <summary>
/// DTO representing standard application health status.
/// Matches the expected Phase 1 API specification:
/// {
///   "status": "Healthy",
///   "application": "ERMS.API"
/// }
/// </summary>
public class HealthResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Healthy";

    [JsonPropertyName("application")]
    public string Application { get; set; } = "ERMS.API";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("environment")]
    public string? Environment { get; set; }
}

/// <summary>
/// DTO representing database connectivity verification.
/// Returns connectivity status without exposing sensitive credentials or internal secrets.
/// </summary>
public class DatabaseHealthResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "Healthy";

    [JsonPropertyName("application")]
    public string Application { get; set; } = "ERMS.API";

    [JsonPropertyName("database")]
    public string Database { get; set; } = "erms_db";

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "PostgreSQL (Npgsql)";

    [JsonPropertyName("connected")]
    public bool Connected { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
