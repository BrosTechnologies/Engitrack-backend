namespace Engitrack.Api.Auth;

public record RegisterRequest(string Email, string FullName, string Phone, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(Guid UserId, string Email, string Role, string AccessToken);

// User Profile DTOs
public record UserProfileResponse(Guid Id, string Email, string FullName, string Phone, string Role);
public record UpdateUserProfileRequest(string FullName, string Phone);
public record UserStatsResponse(int ProjectsCount, int TasksCount, int CompletedTasksCount);