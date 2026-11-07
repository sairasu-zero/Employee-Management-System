using System.Globalization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using ERMS.API.Data;
using ERMS.API.DTOs;
using ERMS.API.Interfaces;
using ERMS.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace ERMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totpService;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        ApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITotpService totpService,
        IEncryptionService encryptionService,
        IAuditService auditService,
        ILogger<AuthController> logger,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponseDto(false, "Email and password are required."));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            await EnsureDatabaseSeededAsync(cancellationToken);

            var user = await _dbContext.Users
                .Include(u => u.Role)
                .ThenInclude(r => r!.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
                .Include(u => u.MfaConfiguration)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() || u.Username.ToLower() == request.Email.ToLower(), cancellationToken);

            if (user == null)
            {
                await _auditService.LogLoginAttemptAsync(request.Email, false, "UserNotFound", ip, cancellationToken);
                return Unauthorized(new AuthResponseDto(false, "Invalid credentials. Please verify your email and password."));
            }

            if (!user.IsActive)
            {
                await _auditService.LogLoginAttemptAsync(request.Email, false, "AccountInactive", ip, cancellationToken);
                return Unauthorized(new AuthResponseDto(false, "Account is disabled. Please contact your system administrator."));
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                await _auditService.LogLoginAttemptAsync(request.Email, false, "AccountLocked", ip, cancellationToken);
                return StatusCode(StatusCodes.Status423Locked, new AuthResponseDto(false, $"Account is locked until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm} UTC due to multiple failed login attempts."));
            }

            bool passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!passwordValid)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _dbContext.SaveChangesAsync(cancellationToken);

                await _auditService.LogLoginAttemptAsync(request.Email, false, "InvalidPassword", ip, cancellationToken);
                return Unauthorized(new AuthResponseDto(false, "Invalid credentials. Please verify your email and password."));
            }

            // Reset failed login counter on success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            // Check if Multi-Factor Authentication (MFA) is required
            bool mfaRequired = user.RequireMfa || (user.MfaConfiguration != null && user.MfaConfiguration.IsEnabled);
            if (mfaRequired)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Ok(new AuthResponseDto(
                    Success: true,
                    Message: "MFA code required.",
                    RequiresMfa: true,
                    MfaType: "totp"
                ));
            }

            // Issue session cookie
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var userDto = BuildUserDto(user);
            await IssueAuthCookieAsync(user, request.RememberMe);

            await _auditService.LogLoginAttemptAsync(request.Email, true, null, ip, cancellationToken);
            await _auditService.LogActivityAsync(user.Id, user.Email, "Login", "Authentication", "User logged in successfully", "Auth/Session", "Success", ip, cancellationToken);

            return Ok(new AuthResponseDto(true, "Authentication successful", userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login attempt for {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto(false, "An error occurred during authentication. Ensure the database service is reachable."));
        }
    }

    [HttpPost("mfa/verify")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyMfa([FromBody] MfaVerificationRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponseDto(false, "Email and 6-digit verification code are required."));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var user = await _dbContext.Users
                .Include(u => u.Role)
                .ThenInclude(r => r!.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
                .Include(u => u.MfaConfiguration)
                .Include(u => u.RecoveryCodes)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

            if (user == null || user.MfaConfiguration == null)
            {
                return Unauthorized(new AuthResponseDto(false, "Invalid MFA challenge session."));
            }

            bool isValid = false;

            if (request.IsRecoveryCode)
            {
                string codeHash = _totpService.HashRecoveryCode(request.Code);
                var recoveryRecord = user.RecoveryCodes.FirstOrDefault(rc => !rc.IsUsed && rc.CodeHash == codeHash);

                if (recoveryRecord != null)
                {
                    recoveryRecord.IsUsed = true;
                    recoveryRecord.UsedAt = DateTime.UtcNow;
                    isValid = true;
                }
            }
            else
            {
                string decryptedSecret = _encryptionService.Decrypt(user.MfaConfiguration.SecretEncrypted);
                isValid = _totpService.VerifyTotp(decryptedSecret, request.Code);
            }

            if (!isValid)
            {
                await _auditService.LogLoginAttemptAsync(request.Email, false, "InvalidMfaCode", ip, cancellationToken);
                return Unauthorized(new AuthResponseDto(false, "Invalid or expired verification code. Please try again."));
            }

            user.MfaConfiguration.LastVerifiedAt = DateTime.UtcNow;
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var userDto = BuildUserDto(user);
            await IssueAuthCookieAsync(user, request.RememberMe);

            await _auditService.LogLoginAttemptAsync(request.Email, true, null, ip, cancellationToken);
            await _auditService.LogActivityAsync(user.Id, user.Email, "MfaVerified", "Authentication", "MFA verification passed", "Auth/MFA", "Success", ip, cancellationToken);

            return Ok(new AuthResponseDto(true, "MFA verified successfully", userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MFA verification for {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto(false, "An error occurred during MFA verification."));
        }
    }

    [HttpPost("mfa/recovery")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyRecoveryCode([FromBody] RecoveryCodeRequestDto request, CancellationToken cancellationToken)
    {
        return await VerifyMfa(new MfaVerificationRequestDto(
            request.Email,
            request.RecoveryCode,
            IsRecoveryCode: true,
            RememberMe: request.RememberMe), cancellationToken);
    }

    [HttpPost("mfa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(MfaSetupResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupMfa(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .Include(u => u.MfaConfiguration)
            .Include(u => u.RecoveryCodes)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        string secretKey = _totpService.GenerateSecretKey();
        string otpUri = _totpService.GenerateOtpAuthUri(user.Email, secretKey);
        string qrCodeDataUri = _totpService.GenerateQrCodeDataUri(otpUri);
        var rawRecoveryCodes = _totpService.GenerateRecoveryCodes(10);

        string encryptedSecret = _encryptionService.Encrypt(secretKey);

        if (user.MfaConfiguration == null)
        {
            user.MfaConfiguration = new MfaConfiguration
            {
                UserId = user.Id,
                SecretEncrypted = encryptedSecret,
                IsEnabled = true,
                EnrolledAt = DateTime.UtcNow
            };
            _dbContext.MfaConfigurations.Add(user.MfaConfiguration);
        }
        else
        {
            user.MfaConfiguration.SecretEncrypted = encryptedSecret;
            user.MfaConfiguration.IsEnabled = true;
            user.MfaConfiguration.EnrolledAt = DateTime.UtcNow;
        }

        user.RequireMfa = true;

        // Clear existing unused recovery codes and add new set
        _dbContext.RecoveryCodes.RemoveRange(user.RecoveryCodes.Where(rc => !rc.IsUsed));
        foreach (var code in rawRecoveryCodes)
        {
            _dbContext.RecoveryCodes.Add(new RecoveryCode
            {
                UserId = user.Id,
                CodeHash = _totpService.HashRecoveryCode(code),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogActivityAsync(user.Id, user.Email, "MfaEnrolled", "Security", "User enrolled in TOTP 2FA", "User/MFA", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return Ok(new MfaSetupResponseDto(secretKey, otpUri, qrCodeDataUri, rawRecoveryCodes));
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
        {
            return BadRequest(new AuthResponseDto(false, "Full Name, Email, and Password are required."));
        }

        try
        {
            await EnsureDatabaseSeededAsync(cancellationToken);

            bool emailTaken = await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);
            if (emailTaken)
            {
                return BadRequest(new AuthResponseDto(false, "An account with this email address already exists."));
            }

            var (hash, salt) = _passwordHasher.HashPassword(request.Password);

            // Assign default Employee role
            var employeeRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Employee", cancellationToken);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Email.Split('@')[0],
                Email = request.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                RoleId = employeeRole?.Id,
                IsActive = true,
                RequireMfa = false,
                CreatedAt = DateTime.UtcNow
            };

            // Parse Full Name
            var nameParts = request.FullName.Trim().Split(' ', 2);
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[1] : firstName;

            var newEmployee = new Employee
            {
                Id = Guid.NewGuid(),
                EmployeeNumber = string.IsNullOrWhiteSpace(request.EmployeeId) ? $"EMP-{Random.Shared.Next(1000, 9999)}" : request.EmployeeId,
                FirstName = firstName,
                LastName = lastName,
                Email = request.Email,
                Position = "New Hire",
                EmploymentType = "FullTime",
                HireDate = DateTime.UtcNow,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            newUser.EmployeeId = newEmployee.Id;
            newUser.Employee = newEmployee;

            await _dbContext.Employees.AddAsync(newEmployee, cancellationToken);
            await _dbContext.Users.AddAsync(newUser, cancellationToken);

            if (employeeRole != null)
            {
                await _dbContext.UserRoles.AddAsync(new UserRole
                {
                    UserId = newUser.Id,
                    RoleId = employeeRole.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = "SystemRegistration"
                }, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await IssueAuthCookieAsync(newUser, false);
            var userDto = BuildUserDto(newUser);

            await _auditService.LogActivityAsync(newUser.Id, newUser.Email, "Register", "Authentication", "New user registered", "Auth/Register", "Success", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

            return Ok(new AuthResponseDto(true, "Registration successful", userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new AuthResponseDto(false, "Failed to register new account."));
        }
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users
            .Include(u => u.Role)
            .ThenInclude(r => r!.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Include(u => u.Employee)
            .ThenInclude(e => e!.Department)
            .Include(u => u.MfaConfiguration)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(BuildUserDto(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrWhiteSpace(email))
        {
            await _auditService.LogActivityAsync(null, email, "Logout", "Authentication", "User logged out", "Auth/Session", "Success", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        return Ok(new { success = true, message = "Logged out successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { success = false, message = "Email is required." });
        }

        string? developmentResetToken = null;
        if (_environment.IsDevelopment())
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(account => account.Email.ToLower() == normalizedEmail && account.IsActive, cancellationToken);

            if (user != null)
            {
                developmentResetToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                user.PasswordResetTokenHash = HashResetToken(developmentResetToken);
                user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30);
                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        await _auditService.LogActivityAsync(
            null,
            null,
            "PasswordResetRequested",
            "Authentication",
            "Password reset request processed",
            "Auth/Recovery",
            "Accepted",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(new ForgotPasswordResponseDto(
            true,
            "If the account exists, the password reset request has been accepted.",
            developmentResetToken));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8 ||
            request.Password.Length > 128)
        {
            return BadRequest(new { success = false, message = "A reset token and a password between 8 and 128 characters are required." });
        }

        var now = DateTime.UtcNow;
        var tokenHash = HashResetToken(request.Token);
        var account = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.PasswordResetTokenHash == tokenHash &&
                user.PasswordResetTokenExpiresAtUtc > now &&
                user.IsActive)
            .Select(user => new { user.Id, user.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (account == null)
        {
            await LogPasswordResetFailureAsync(cancellationToken);
            return BadRequest(new { success = false, message = "The password reset link is invalid or expired." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var claimedCount = await _dbContext.Users
            .Where(user => user.Id == account.Id &&
                user.PasswordResetTokenHash == tokenHash &&
                user.PasswordResetTokenExpiresAtUtc > now &&
                user.IsActive)
            .ExecuteUpdateAsync(
                update => update
                    .SetProperty(user => user.PasswordResetTokenHash, (string?)null)
                    .SetProperty(user => user.PasswordResetTokenExpiresAtUtc, (DateTime?)null),
                cancellationToken);

        if (claimedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            await LogPasswordResetFailureAsync(cancellationToken);
            return BadRequest(new { success = false, message = "The password reset link is invalid or expired." });
        }

        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.Password);
        var updatedCount = await _dbContext.Users
            .Where(user => user.Id == account.Id && user.IsActive)
            .ExecuteUpdateAsync(update => update
                .SetProperty(user => user.PasswordHash, passwordHash)
                .SetProperty(user => user.PasswordSalt, passwordSalt)
                .SetProperty(user => user.SessionVersion, user => user.SessionVersion + 1)
                .SetProperty(user => user.FailedLoginAttempts, 0)
                .SetProperty(user => user.LockoutEnd, (DateTime?)null)
                .SetProperty(user => user.UpdatedAt, now),
                cancellationToken);

        if (updatedCount != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            await LogPasswordResetFailureAsync(cancellationToken);
            return BadRequest(new { success = false, message = "The password reset link is invalid or expired." });
        }

        await _dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == account.Id && refreshToken.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(refreshToken => refreshToken.RevokedAt, now), cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _auditService.LogActivityAsync(
            account.Id,
            account.Email,
            "PasswordResetCompleted",
            "Authentication",
            "Password reset completed",
            "Auth/Recovery",
            "Success",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(new { success = true, message = "Password updated. You can now sign in." });
    }

    private async Task IssueAuthCookieAsync(User user, bool rememberMe)
    {
        var roleName = user.Role?.Name ?? "Employee";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("session_version", user.SessionVersion.ToString(CultureInfo.InvariantCulture)),
        };

        if (user.Role?.RolePermissions != null)
        {
            foreach (var rp in user.Role.RolePermissions)
            {
                if (rp.Permission != null)
                {
                    claims.Add(new Claim("Permission", rp.Permission.Name));
                }
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private static string HashResetToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private Task LogPasswordResetFailureAsync(CancellationToken cancellationToken)
    {
        return _auditService.LogActivityAsync(
            null,
            null,
            "PasswordResetFailed",
            "Authentication",
            "Invalid or expired password reset token submitted",
            "Auth/Recovery",
            "Denied",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
    }

    private static UserDto BuildUserDto(User user)
    {
        string roleName = user.Role?.Name ?? "Employee";
        string fullName = user.Employee != null
            ? $"{user.Employee.FirstName} {user.Employee.LastName}".Trim()
            : user.Username;

        string initials = "U";
        if (user.Employee != null && !string.IsNullOrWhiteSpace(user.Employee.FirstName) && !string.IsNullOrWhiteSpace(user.Employee.LastName))
        {
            initials = $"{user.Employee.FirstName[0]}{user.Employee.LastName[0]}".ToUpper();
        }
        else if (!string.IsNullOrWhiteSpace(user.Username))
        {
            initials = user.Username.Length >= 2 ? user.Username[..2].ToUpper() : user.Username.ToUpper();
        }

        var permissions = user.Role?.RolePermissions?
            .Select(rp => rp.Permission?.Name ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        bool isMfa = user.RequireMfa || (user.MfaConfiguration != null && user.MfaConfiguration.IsEnabled);

        return new UserDto(
            Id: user.Id.ToString(),
            EmployeeId: user.Employee?.EmployeeNumber ?? "EMP-ADMIN",
            FullName: fullName,
            Email: user.Email,
            Role: roleName,
            Department: user.Employee?.Department?.Name ?? "Executive Administration",
            Position: user.Employee?.Position ?? (roleName == "Admin" ? "Chief Systems Administrator" : "Staff"),
            AvatarUrl: null,
            AvatarInitials: initials,
            Permissions: permissions,
            IsMfaEnabled: isMfa,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt
        );
    }

    private async Task EnsureDatabaseSeededAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        // 1. Seed Roles
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "System Administrator", IsSystemRole = true };
        var hrRole = new Role { Id = Guid.NewGuid(), Name = "HR", Description = "Human Resources Officer", IsSystemRole = true };
        var managerRole = new Role { Id = Guid.NewGuid(), Name = "Manager", Description = "Departmental Manager", IsSystemRole = true };
        var employeeRole = new Role { Id = Guid.NewGuid(), Name = "Employee", Description = "Standard Employee Self-Service", IsSystemRole = true };

        await _dbContext.Roles.AddRangeAsync(new[] { adminRole, hrRole, managerRole, employeeRole }, cancellationToken);

        // 2. Seed Permissions
        var permissions = new List<Permission>
        {
            new Permission { Id = Guid.NewGuid(), Module = "Users", Action = "Read", Name = "users:read", Description = "View users" },
            new Permission { Id = Guid.NewGuid(), Module = "Users", Action = "Write", Name = "users:write", Description = "Manage users" },
            new Permission { Id = Guid.NewGuid(), Module = "Roles", Action = "Manage", Name = "roles:manage", Description = "Manage roles and permissions" },
            new Permission { Id = Guid.NewGuid(), Module = "Employees", Action = "Read", Name = "employees:read", Description = "View employee directory" },
            new Permission { Id = Guid.NewGuid(), Module = "Employees", Action = "Write", Name = "employees:write", Description = "Create and modify employees" },
            new Permission { Id = Guid.NewGuid(), Module = "Departments", Action = "Manage", Name = "departments:manage", Description = "Manage departments" },
            new Permission { Id = Guid.NewGuid(), Module = "Audit", Action = "Read", Name = "audit:read", Description = "View security audit trails" },
            new Permission { Id = Guid.NewGuid(), Module = "Leaves", Action = "Request", Name = "leaves:request", Description = "Submit leave requests" },
            new Permission { Id = Guid.NewGuid(), Module = "Leaves", Action = "Approve", Name = "team:approve_leave", Description = "Approve department leaves" },
        };

        await _dbContext.Permissions.AddRangeAsync(permissions, cancellationToken);

        // Link Admin to all permissions
        foreach (var p in permissions)
        {
            await _dbContext.RolePermissions.AddAsync(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id }, cancellationToken);
        }

        // 3. Seed Default Department
        var adminDept = new Department { Id = Guid.NewGuid(), Name = "Executive Administration", Code = "ADM", Description = "Corporate Administration" };
        var engDept = new Department { Id = Guid.NewGuid(), Name = "Software Engineering", Code = "ENG", Description = "Core Technology & Software Development" };
        var hrDept = new Department { Id = Guid.NewGuid(), Name = "Human Resources", Code = "HR", Description = "People Operations & Talent Acquisition" };

        await _dbContext.Departments.AddRangeAsync(new[] { adminDept, engDept, hrDept }, cancellationToken);

        // 4. Seed Seed User (Admin)
        var (hash, salt) = _passwordHasher.HashPassword("Admin@123456");
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@erms.local",
            PasswordHash = hash,
            PasswordSalt = salt,
            RoleId = adminRole.Id,
            IsActive = true,
            RequireMfa = false,
            CreatedAt = DateTime.UtcNow
        };

        var adminEmp = new Employee
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP-ADM-01",
            FirstName = "Alexander",
            LastName = "Mitchell",
            Email = "admin@erms.local",
            DepartmentId = adminDept.Id,
            Position = "Chief Systems Administrator",
            EmploymentType = "FullTime",
            HireDate = DateTime.UtcNow,
            Status = "Active"
        };

        adminUser.EmployeeId = adminEmp.Id;

        await _dbContext.Employees.AddAsync(adminEmp, cancellationToken);
        await _dbContext.Users.AddAsync(adminUser, cancellationToken);
        await _dbContext.UserRoles.AddAsync(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
