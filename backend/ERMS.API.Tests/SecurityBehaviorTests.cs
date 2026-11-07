using ERMS.API.Security;
using ERMS.API.Controllers;
using ERMS.API.Data;
using ERMS.API.DTOs;
using ERMS.API.Interfaces;
using ERMS.API.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ERMS.API.Tests;

public class SecurityBehaviorTests
{
    private static readonly string[] DevelopmentOrigins =
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5174",
        "http://127.0.0.1:5174",
        "http://localhost:3000",
        "http://localhost:5000",
        "https://localhost:5001"
    ];

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://127.0.0.1:5174")]
    public async Task UnsafeRequest_AllowsConfiguredDevelopmentOrigin(string origin)
    {
        var result = await InvokeOriginMiddlewareAsync("POST", origin);

        Assert.True(result.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Theory]
    [InlineData("https://localhost:5173")]
    [InlineData("http://localhost.evil.example:5173")]
    [InlineData("http://localhost:5173/unexpected-path")]
    public async Task UnsafeRequest_RejectsUnconfiguredOrMalformedOrigin(string origin)
    {
        var result = await InvokeOriginMiddlewareAsync("POST", origin);

        Assert.False(result.NextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task UnsafeRequest_RejectsMissingOriginAndReferer()
    {
        var result = await InvokeOriginMiddlewareAsync("POST", null);

        Assert.False(result.NextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task UnsafeRequest_AcceptsAllowedRefererWhenOriginIsAbsent()
    {
        var result = await InvokeOriginMiddlewareAsync(
            "POST",
            origin: null,
            referer: "http://localhost:5173/login");

        Assert.True(result.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task SafeRequest_DoesNotRequireOrigin(string method)
    {
        var result = await InvokeOriginMiddlewareAsync(method, null);

        Assert.True(result.NextCalled);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("https://attacker.example", false)]
    public async Task WebSocketHandshake_RequiresConfiguredOrigin(string origin, bool expectedNextCalled)
    {
        var result = await InvokeOriginMiddlewareAsync("GET", origin, websocketUpgrade: true);

        Assert.Equal(expectedNextCalled, result.NextCalled);
        Assert.Equal(expectedNextCalled ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Theory]
    [InlineData(true, CookieSecurePolicy.SameAsRequest)]
    [InlineData(false, CookieSecurePolicy.Always)]
    public void CookieConfiguration_UsesSecureDefaults(bool isDevelopment, CookieSecurePolicy expectedSecurePolicy)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddErmsCookieAuthentication(isDevelopment);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("ERMS.Session", options.Cookie.Name);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal(expectedSecurePolicy, options.Cookie.SecurePolicy);
    }

    [Theory]
    [InlineData("/api/auth/login", "{\"email\":\"user@example.test\",\"password\":\"invalid\"}")]
    [InlineData("/api/auth/logout", "{}")]
    [InlineData("/api/auth/reset-password", "{\"token\":\"invalid\",\"password\":\"invalid\"}")]
    public async Task AuthMutation_RejectsUntrustedOriginBeforeEndpoint(string path, string body)
    {
        using var factory = new ErmsApiFactory("Development");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Origin", "https://attacker.example");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentFrontendCanPreflightAndLogoutWithCredentialedCors()
    {
        using var factory = new ErmsApiFactory("Development");
        using var client = factory.CreateClient();
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/auth/logout");
        preflight.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        preflight.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        preflight.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "content-type");

        using var preflightResponse = await client.SendAsync(preflight);

        Assert.Equal(HttpStatusCode.NoContent, preflightResponse.StatusCode);
        Assert.Equal("http://localhost:5173", preflightResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", preflightResponse.Headers.GetValues("Access-Control-Allow-Credentials").Single());

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");

        using var logoutResponse = await client.SendAsync(logout);

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.Equal("http://localhost:5173", logoutResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains(logoutResponse.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith("ERMS.Session=", StringComparison.Ordinal) &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
            cookie.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("https://localhost:5001")]
    public async Task DevelopmentApiOriginsCanMakeSameOriginPostRequests(string origin)
    {
        using var factory = new ErmsApiFactory("Development");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.TryAddWithoutValidation("Origin", origin);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProductionRequiresConfiguredOriginAndUsesSecureCookies()
    {
        using var factory = new ErmsApiFactory("Production");
        var cookieOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedApiRequestReturns401InsteadOfLoginRedirect()
    {
        using var factory = new ErmsApiFactory("Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task DevelopmentReset_UsesHashedSingleUseTokenAndRevokesExistingSession()
    {
        using var factory = new ErmsApiFactory("Development", useSqlite: true);
        using var client = factory.CreateClient();
        const string email = "reset-target@erms.local";
        const string oldPassword = "OldPassword#2026";
        const string newPassword = "NewPassword#2026";

        await SeedAccountAsync(factory, email, oldPassword);

        using var loginResponse = await PostJsonAsync(client, "/api/auth/login", new
        {
            email,
            password = oldPassword,
            rememberMe = true
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var currentUserResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUserResponse.StatusCode);

        using var unknownRequestResponse = await PostJsonAsync(client, "/api/auth/forgot-password", new
        {
            email = "missing@erms.local"
        });
        using var knownRequestResponse = await PostJsonAsync(client, "/api/auth/forgot-password", new { email });
        var unknownRequest = await unknownRequestResponse.Content.ReadFromJsonAsync<ForgotPasswordResponseDto>();
        var knownRequest = await knownRequestResponse.Content.ReadFromJsonAsync<ForgotPasswordResponseDto>();

        Assert.Equal(HttpStatusCode.OK, unknownRequestResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, knownRequestResponse.StatusCode);
        Assert.Equal(unknownRequest!.Message, knownRequest!.Message);
        Assert.Null(unknownRequest.DevelopmentResetToken);
        var resetToken = Assert.IsType<string>(knownRequest.DevelopmentResetToken);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Email == email);
            var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resetToken)));
            Assert.Equal(expectedHash, account.PasswordResetTokenHash);
            Assert.InRange(account.PasswordResetTokenExpiresAtUtc!.Value, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        }

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbContext.Users.SingleAsync(user => user.Email == email);
            dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = account.Id,
                TokenHash = "legacy-active-refresh-token-hash",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await dbContext.SaveChangesAsync();
        }

        using var resetResponse = await PostJsonAsync(client, "/api/auth/reset-password", new
        {
            token = resetToken,
            email,
            password = newPassword
        });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Email == email);
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            Assert.True(hasher.VerifyPassword(newPassword, account.PasswordHash));
            Assert.False(hasher.VerifyPassword(oldPassword, account.PasswordHash));
            Assert.Null(account.PasswordResetTokenHash);
            Assert.Null(account.PasswordResetTokenExpiresAtUtc);
            Assert.Equal(1, account.SessionVersion);
            var refreshToken = await dbContext.RefreshTokens.SingleAsync(token => token.UserId == account.Id);
            Assert.NotNull(refreshToken.RevokedAt);

            var auditLogs = await dbContext.AuditLogs.AsNoTracking().ToListAsync();
            Assert.Contains(auditLogs, entry => entry.ActivityType == "PasswordResetCompleted");
            Assert.DoesNotContain(auditLogs, entry =>
                entry.Description.Contains(resetToken, StringComparison.Ordinal) ||
                entry.TargetResource?.Contains(resetToken, StringComparison.Ordinal) == true);
        }

        using var revokedSessionResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedSessionResponse.StatusCode);

        using var replayResponse = await PostJsonAsync(client, "/api/auth/reset-password", new
        {
            token = resetToken,
            email,
            password = "AnotherPassword#2026"
        });
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);
    }

    [Fact]
    public async Task ExpiredResetTokenCannotChangePassword()
    {
        using var factory = new ErmsApiFactory("Development", useSqlite: true);
        using var client = factory.CreateClient();
        const string email = "expired-reset@erms.local";
        const string password = "ExistingPassword#2026";
        await SeedAccountAsync(factory, email, password);

        using var requestResponse = await PostJsonAsync(client, "/api/auth/forgot-password", new { email });
        var resetRequest = await requestResponse.Content.ReadFromJsonAsync<ForgotPasswordResponseDto>();
        var resetToken = Assert.IsType<string>(resetRequest!.DevelopmentResetToken);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var account = await dbContext.Users.SingleAsync(user => user.Email == email);
            account.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        using var resetResponse = await PostJsonAsync(client, "/api/auth/reset-password", new
        {
            token = resetToken,
            password = "ReplacementPassword#2026"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resetResponse.StatusCode);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unchangedAccount = await verifyDbContext.Users.AsNoTracking().SingleAsync(user => user.Email == email);
        var passwordHasher = verifyScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        Assert.True(passwordHasher.VerifyPassword(password, unchangedAccount.PasswordHash));
    }

    [Fact]
    public async Task ProductionResetRequestDoesNotExposeOrStoreTokenAndKeepsGenericResponse()
    {
        using var factory = new ErmsApiFactory("Production", useSqlite: true);
        const string email = "production-reset@erms.local";
        await SeedAccountAsync(factory, email, "ExistingPassword#2026");

        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var controller = new AuthController(
            services.GetRequiredService<ApplicationDbContext>(),
            services.GetRequiredService<IPasswordHasher>(),
            services.GetRequiredService<ITotpService>(),
            services.GetRequiredService<IEncryptionService>(),
            services.GetRequiredService<IAuditService>(),
            services.GetRequiredService<ILogger<AuthController>>(),
            services.GetRequiredService<IWebHostEnvironment>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services }
            }
        };

        var knownResponse = Assert.IsType<OkObjectResult>(await controller.ForgotPassword(
            new ForgotPasswordRequestDto(email), CancellationToken.None));
        var unknownResponse = Assert.IsType<OkObjectResult>(await controller.ForgotPassword(
            new ForgotPasswordRequestDto("missing@erms.local"), CancellationToken.None));
        var knownBody = Assert.IsType<ForgotPasswordResponseDto>(knownResponse.Value);
        var unknownBody = Assert.IsType<ForgotPasswordResponseDto>(unknownResponse.Value);

        Assert.Equal(knownBody.Message, unknownBody.Message);
        Assert.Null(knownBody.DevelopmentResetToken);
        Assert.Null(unknownBody.DevelopmentResetToken);

        var account = await services.GetRequiredService<ApplicationDbContext>()
            .Users.AsNoTracking().SingleAsync(user => user.Email == email);
        Assert.Null(account.PasswordResetTokenHash);
        Assert.Null(account.PasswordResetTokenExpiresAtUtc);
    }

    private static async Task<(bool NextCalled, int StatusCode)> InvokeOriginMiddlewareAsync(
        string method,
        string? origin,
        string? referer = null,
        bool websocketUpgrade = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        if (referer is not null)
        {
            context.Request.Headers.Referer = referer;
        }

        if (websocketUpgrade)
        {
            context.Request.Headers["Upgrade"] = "websocket";
            context.Request.Headers["Connection"] = "keep-alive, Upgrade";
        }

        var nextCalled = false;
        var middleware = new UnsafeRequestOriginMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            DevelopmentOrigins);

        await middleware.InvokeAsync(context);
        return (nextCalled, context.Response.StatusCode);
    }

    private static async Task SeedAccountAsync(ErmsApiFactory factory, string email, string password)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var (passwordHash, passwordSalt) = passwordHasher.HashPassword(password);
        dbContext.Users.Add(new User
        {
            Username = email,
            Email = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        return await client.SendAsync(request);
    }

    private sealed class ErmsApiFactory(
        string environment,
        bool useSqlite = false) : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var contentRoot = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "ERMS.API"));

            builder.UseEnvironment(environment);
            builder.UseContentRoot(contentRoot);
            if (useSqlite)
            {
                _connection.Open();
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ApplicationDbContext>();
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}