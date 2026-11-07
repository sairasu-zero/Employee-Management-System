using System.Globalization;
using System.Security.Claims;
using ERMS.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERMS.API.Security;

public static class CookieAuthenticationRegistration
{
    public static AuthenticationBuilder AddErmsCookieAuthentication(
        this IServiceCollection services,
        bool isDevelopment)
    {
        return services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "ERMS.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
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
                options.Events.OnValidatePrincipal = async context =>
                {
                    var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var sessionVersionValue = context.Principal?.FindFirstValue("session_version");
                    if (!Guid.TryParse(userIdValue, out var userId) ||
                        !int.TryParse(sessionVersionValue, NumberStyles.None, CultureInfo.InvariantCulture, out var sessionVersion))
                    {
                        context.RejectPrincipal();
                        return;
                    }

                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var currentSessionVersion = await dbContext.Users
                        .AsNoTracking()
                        .Where(user => user.Id == userId)
                        .Select(user => (int?)user.SessionVersion)
                        .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                    if (currentSessionVersion != sessionVersion)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });
    }
}