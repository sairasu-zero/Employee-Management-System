using ERMS.API.Configuration;
using ERMS.API.Data;
using ERMS.API.Hubs;
using ERMS.API.Interfaces;
using ERMS.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        Description = "ASP.NET Core Web API for ERMS - Authentication, RBAC, Workforce Management & Audit Trail"
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

// 4. Register Security & Core Infrastructure Services
builder.Services.AddDataProtection();
builder.Services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();

// 5. Configure Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ERMS.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// 6. Configure Development CORS Policy
const string developmentCorsPolicy = "DevelopmentCorsPolicy";

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5174",
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

// 7. Configure Middleware Pipeline
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

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API Controllers & Real-Time SignalR Hub
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Root route redirects to Swagger documentation
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
