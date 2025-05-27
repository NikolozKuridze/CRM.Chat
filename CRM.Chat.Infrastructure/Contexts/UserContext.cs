using Microsoft.AspNetCore.Http;

namespace CRM.Chat.Infrastructure.Contexts;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid Id => GetUserId();

    public string? UserName => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public string? Email => _httpContextAccessor.HttpContext?.User.Claims
        .FirstOrDefault(c => c.Type == "Email")?.Value;

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ??
        Enumerable.Empty<string>();

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    private Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.Claims
            .FirstOrDefault(c =>
                string.Equals(c.Type, "Uid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "sub", StringComparison.OrdinalIgnoreCase) ||
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}