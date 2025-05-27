using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Hubs.Core;

[Authorize]
public class OperatorHub : Hub
{
    private readonly ILogger<OperatorHub> _logger;

    public OperatorHub(ILogger<OperatorHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("Operator {UserId} connected to OperatorHub with connection {ConnectionId}",
            userId, Context.ConnectionId);

        // Add to operators group
        await Groups.AddToGroupAsync(Context.ConnectionId, "Operators");

        // TODO: Check if user is admin and add to administrators group
        // var isAdmin = await CheckIfUserIsAdmin(userId);
        // if (isAdmin)
        // {
        //     await Groups.AddToGroupAsync(Context.ConnectionId, "Administrators");
        // }

        // Set operator as online in Redis
        // This would typically be handled by a service
        await NotifyOperatorOnline(userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("Operator {UserId} disconnected from OperatorHub. Connection {ConnectionId}",
            userId, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "Operator {UserId} disconnected due to error", userId);
        }

        // Set operator as offline
        await NotifyOperatorOffline(userId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SetStatus(string status)
    {
        var userId = GetUserId();

        // This would typically trigger a command through the mediator
        _logger.LogInformation("Operator {UserId} set status to {Status}", userId, status);

        // Broadcast status change to administrators
        await Clients.Group("Administrators").SendAsync("OperatorStatusChanged", new
        {
            OperatorId = userId,
            Status = status,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public async Task AcceptChat(string chatId)
    {
        if (Guid.TryParse(chatId, out var parsedChatId))
        {
            var userId = GetUserId();

            _logger.LogInformation("Operator {UserId} accepted chat {ChatId}", userId, parsedChatId);

            // This would typically trigger an assignment command
            await Clients.User(userId.ToString()).SendAsync("ChatAccepted", new
            {
                ChatId = parsedChatId,
                AcceptedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public async Task RequestChatTransfer(string chatId, string targetOperatorId, string reason)
    {
        if (Guid.TryParse(chatId, out var parsedChatId) &&
            Guid.TryParse(targetOperatorId, out var parsedTargetOperatorId))
        {
            var userId = GetUserId();

            _logger.LogInformation("Operator {UserId} requested transfer of chat {ChatId} to {TargetOperatorId}",
                userId, parsedChatId, parsedTargetOperatorId);

            // This would typically trigger a transfer command
            await Clients.Group("Administrators").SendAsync("TransferRequested", new
            {
                ChatId = parsedChatId,
                FromOperatorId = userId,
                ToOperatorId = parsedTargetOperatorId,
                Reason = reason,
                RequestedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public async Task JoinAdminDashboard()
    {
        var userId = GetUserId();

        // TODO: Verify admin role
        await Groups.AddToGroupAsync(Context.ConnectionId, "Administrators");

        _logger.LogInformation("User {UserId} joined admin dashboard", userId);
    }

    private async Task NotifyOperatorOnline(Guid operatorId)
    {
        // Broadcast to other operators and admins
        await Clients.Group("Operators").SendAsync("OperatorOnline", new
        {
            OperatorId = operatorId,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Clients.Group("Administrators").SendAsync("OperatorOnline", new
        {
            OperatorId = operatorId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private async Task NotifyOperatorOffline(Guid operatorId)
    {
        // Broadcast to other operators and admins
        await Clients.Group("Operators").SendAsync("OperatorOffline", new
        {
            OperatorId = operatorId,
            Timestamp = DateTimeOffset.UtcNow
        });

        await Clients.Group("Administrators").SendAsync("OperatorOffline", new
        {
            OperatorId = operatorId,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.Claims
            .FirstOrDefault(c => c.Type == "Uid" || c.Type == "sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}