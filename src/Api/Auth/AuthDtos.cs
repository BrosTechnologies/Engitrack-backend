namespace Engitrack.Api.Auth;

public record RegisterRequest(string Email, string FullName, string Phone, string Password, string Role);
public record LoginRequest(string Email, string Password);
public record AuthResponse(Guid UserId, string Email, string Role, string AccessToken);