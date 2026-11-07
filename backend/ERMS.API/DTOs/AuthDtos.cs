namespace ERMS.API.DTOs;

public record LoginRequestDto(
    string Email,
    string Password,
    bool RememberMe = false
);

public record RegisterRequestDto(
    string FullName,
    string EmployeeId,
    string Email,
    string Department,
    string Password,
    string? ConfirmPassword = null
);

public record MfaVerificationRequestDto(
    string Email,
    string Code,
    bool IsRecoveryCode = false
);

public record RecoveryCodeRequestDto(
    string Email,
    string RecoveryCode
);

public record ForgotPasswordRequestDto(
    string Email
);

public record ResetPasswordRequestDto(
    string Token,
    string? Email,
    string Password,
    string? ConfirmPassword = null
);

public record UserDto(
    string Id,
    string? EmployeeId,
    string FullName,
    string Email,
    string Role,
    string? Department,
    string? Position,
    string? AvatarUrl,
    string? AvatarInitials,
    List<string>? Permissions,
    bool IsMfaEnabled,
    DateTime? CreatedAt,
    DateTime? LastLoginAt
);

public record AuthResponseDto(
    bool Success,
    string? Message = null,
    UserDto? User = null,
    string? Token = null,
    bool RequiresMfa = false,
    string? MfaType = null
);

public record MfaSetupResponseDto(
    string SecretKey,
    string OtpAuthUri,
    string QrCodeDataUri,
    List<string> RecoveryCodes
);
