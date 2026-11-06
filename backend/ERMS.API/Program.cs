using ERMS.API.Configuration;
using ERMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services and Dependency Injection
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Configure Swagger / OpenAPI Documentation
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Records Management System (ERMS) API",
        Version = "v1",
        Description = "ASP.NET Core Web API for ERMS - Phase 1 Development Foundation"
    });
});

// 3. Configure PostgreSQL Database with Entity Framework Core & Npgsql
var connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Configure retry on failure for transient PostgreSQL errors
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// 4. Configure Development CORS Policy
// Restricted specifically to frontend development origins
const string developmentCorsPolicy = "DevelopmentCorsPolicy";

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:3000"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy(developmentCorsPolicy, policyBuilder =>
    {
        policyBuilder
            .WithOrigins(configuredOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// 5. Configure Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERMS.API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();

// Enable configured CORS
app.UseCors(developmentCorsPolicy);

// Map API Controllers
app.MapControllers();

app.Run();
