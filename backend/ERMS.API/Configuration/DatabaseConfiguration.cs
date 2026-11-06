using Npgsql;

namespace ERMS.API.Configuration;

/// <summary>
/// Helper for resolving the PostgreSQL connection string securely from Configuration,
/// Environment Variables, or User Secrets.
/// </summary>
public static class DatabaseConfiguration
{
    public const string DefaultDatabaseName = "erms_db";

    public static string GetConnectionString(IConfiguration configuration)
    {
        // 1. Check for standard connection string in configuration / User Secrets / env vars
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var builder = !string.IsNullOrWhiteSpace(connectionString)
            ? new NpgsqlConnectionStringBuilder(connectionString)
            : new NpgsqlConnectionStringBuilder();

        // 2. Allow environment variables to override or populate missing credentials
        var host = Environment.GetEnvironmentVariable("DB_HOST");
        if (!string.IsNullOrWhiteSpace(host))
        {
            builder.Host = host;
        }
        else if (string.IsNullOrWhiteSpace(builder.Host))
        {
            builder.Host = "localhost";
        }

        var portStr = Environment.GetEnvironmentVariable("DB_PORT");
        if (!string.IsNullOrWhiteSpace(portStr) && int.TryParse(portStr, out var port))
        {
            builder.Port = port;
        }
        else if (builder.Port == 0)
        {
            builder.Port = 5432;
        }

        var database = Environment.GetEnvironmentVariable("DB_NAME");
        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.Database = database;
        }
        else if (string.IsNullOrWhiteSpace(builder.Database))
        {
            builder.Database = DefaultDatabaseName;
        }

        var username = Environment.GetEnvironmentVariable("DB_USER");
        if (!string.IsNullOrWhiteSpace(username))
        {
            builder.Username = username;
        }
        else if (string.IsNullOrWhiteSpace(builder.Username))
        {
            builder.Username = "postgres";
        }

        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") 
                       ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        if (!string.IsNullOrWhiteSpace(password))
        {
            builder.Password = password;
        }

        return builder.ConnectionString;
    }
}
