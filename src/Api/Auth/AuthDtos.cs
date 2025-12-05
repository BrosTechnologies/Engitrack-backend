namespace Engitrack.Api.Auth;

public record RegisterRequest(string Email, string FullName, string Phone, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(Guid UserId, string Email, string Role, string AccessToken);

// User Profile DTOs
public record UserProfileResponse(Guid Id, string Email, string FullName, string Phone, string Role);
public record UpdateUserProfileRequest(string FullName, string Phone);
public record UserStatsResponse(int ProjectsCount, int TasksCount, int CompletedTasksCount);

// Password Reset DTOs
public record ForgotPasswordRequest(string Email);
public record ForgotPasswordResponse(string Message);
public record VerifyResetCodeRequest(string Email, string Code);
public record VerifyResetCodeResponse(bool Valid);
public record ResetPasswordRequest(string Email, string Code, string NewPassword);