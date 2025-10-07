using System.Security.Claims;

namespace Engitrack.Api.Security;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsAuthenticated { get; }
    string? Email { get; }
    string? Role { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    public Guid? Id { get; }
    public bool IsAuthenticated => Id.HasValue;
    public string? Email { get; }
    public string? Role { get; }

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
        Email = user?.FindFirst(ClaimTypes.Email)?.Value ?? user?.FindFirst("email")?.Value;
        Role = user?.FindFirst(ClaimTypes.Role)?.Value ?? user?.FindFirst("role")?.Value;

        if (Guid.TryParse(sub, out var id))
            Id = id;
    }
}