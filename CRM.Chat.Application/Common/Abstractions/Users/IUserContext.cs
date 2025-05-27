namespace CRM.Chat.Application.Common.Abstractions.Users;

public interface IUserContext
{
    Guid Id { get; }
    string? UserName { get; }
    string? Email { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    bool IsInRole(string role);
}