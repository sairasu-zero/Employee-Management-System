using ERMS.API.Configuration;
using ERMS.API.Data;
using ERMS.API.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Basic API health endpoint to verify the Web API is running.
    /// Expected response:
    /// {
    ///   "status": "Healthy",
    ///   "application": "ERMS.API"
    /// }
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new HealthResponseDto
        {
            Status = "Healthy",
            Application = "ERMS.API",
            Timestamp = DateTime.UtcNow,
            Environment = _environment.EnvironmentName
        });
    }

    /// <summary>
    /// Database connectivity verification endpoint.
    /// Safely checks connectivity to PostgreSQL without exposing credentials or internal connection strings.
    /// </summary>
    [HttpGet("database")]
    [ProducesResponseType(typeof(DatabaseHealthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DatabaseHealthResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabaseHealth(CancellationToken cancellationToken)
    {
        try
        {
            // Verify PostgreSQL connectivity
            bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return Ok(new DatabaseHealthResponseDto
                {
                    Status = "Healthy",
                    Application = "ERMS.API",
                    Database = DatabaseConfiguration.DefaultDatabaseName,
                    Provider = "PostgreSQL (Npgsql)",
                    Connected = true,
                    Message = "Successfully connected to PostgreSQL database (erms_db).",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogWarning("PostgreSQL database connectivity check returned false.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new DatabaseHealthResponseDto
            {
                Status = "Unhealthy",
                Application = "ERMS.API",
                Database = DatabaseConfiguration.DefaultDatabaseName,
                Provider = "PostgreSQL (Npgsql)",
                Connected = false,
                Message = "Could not establish connection to PostgreSQL database server.",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            // Log full exception internally, but return sanitized error message to caller
            _logger.LogError(ex, "Error occurred while testing PostgreSQL database connectivity.");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new DatabaseHealthResponseDto
            {
                Status = "Unhealthy",
                Application = "ERMS.API",
                Database = DatabaseConfiguration.DefaultDatabaseName,
                Provider = "PostgreSQL (Npgsql)",
                Connected = false,
                Message = "Database connection error: " + ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
