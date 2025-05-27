using CRM.Chat.Infrastructure.Hubs.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Hubs;

public class SignalROperatorHub : IOperatorHub
{
    private readonly IHubContext<OperatorHub> _hubContext;
    private readonly ILogger<SignalROperatorHub> _logger;

    public SignalROperatorHub(IHubContext<OperatorHub> hubContext, ILogger<SignalROperatorHub> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOperatorStatusChangedAsync(Guid operatorId, string status)
    {
        try
        {
            await _hubContext.Clients.User(operatorId.ToString()).SendAsync("StatusChanged", new 
            { 
                OperatorId = operatorId, 
                Status = status, 
                Timestamp = DateTimeOffset.UtcNow 
            });

            // Also broadcast to admin dashboard
            await _hubContext.Clients.Group("Administrators").SendAsync("OperatorStatusUpdate", new 
            { 
                OperatorId = operatorId, 
                Status = status, 
                Timestamp = DateTimeOffset.UtcNow 
            });

            _logger.LogInformation("Operator status change notification sent for operator {OperatorId}: {Status}", 
                operatorId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending operator status change notification for operator {OperatorId}", 
                operatorId);
        }
    }

    public async Task NotifyNewChatAssignmentAsync(Guid operatorId, Guid chatId)
    {
        try
        {
            await _hubContext.Clients.User(operatorId.ToString()).SendAsync("NewChatAssigned", new 
            { 
                ChatId = chatId, 
                AssignedAt = DateTimeOffset.UtcNow 
            });

            _logger.LogInformation("New chat assignment notification sent to operator {OperatorId} for chat {ChatId}", 
                operatorId, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new chat assignment notification to operator {OperatorId} for chat {ChatId}", 
                operatorId, chatId);
        }
    }

    public async Task NotifyChatRemovedAsync(Guid operatorId, Guid chatId)
    {
        try
        {
            await _hubContext.Clients.User(operatorId.ToString()).SendAsync("ChatRemoved", new 
            { 
                ChatId = chatId, 
                RemovedAt = DateTimeOffset.UtcNow 
            });

            _logger.LogInformation("Chat removal notification sent to operator {OperatorId} for chat {ChatId}", 
                operatorId, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat removal notification to operator {OperatorId} for chat {ChatId}", 
                operatorId, chatId);
        }
    }

    public async Task SendOperatorWorkloadUpdateAsync(Guid operatorId, double workloadPercentage)
    {
        try
        {
            await _hubContext.Clients.User(operatorId.ToString()).SendAsync("WorkloadUpdate", new 
            { 
                WorkloadPercentage = workloadPercentage, 
                Timestamp = DateTimeOffset.UtcNow 
            });

            // Also send to admin dashboard
            await _hubContext.Clients.Group("Administrators").SendAsync("OperatorWorkloadUpdate", new 
            { 
                OperatorId = operatorId, 
                WorkloadPercentage = workloadPercentage, 
                Timestamp = DateTimeOffset.UtcNow 
            });

            _logger.LogDebug("Workload update sent to operator {OperatorId}: {WorkloadPercentage}%", 
                operatorId, workloadPercentage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending workload update to operator {OperatorId}", operatorId);
        }
    }

    public async Task BroadcastOperatorAvailabilityAsync(Guid operatorId, bool isAvailable)
    {
        try
        {
            await _hubContext.Clients.Group("Operators").SendAsync("OperatorAvailabilityChanged", new 
            { 
                OperatorId = operatorId, 
                IsAvailable = isAvailable, 
                Timestamp = DateTimeOffset.UtcNow 
            });

            _logger.LogInformation("Operator availability broadcast sent for operator {OperatorId}: {IsAvailable}", 
                operatorId, isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting operator availability for operator {OperatorId}", operatorId);
        }
    }

    public async Task SendNotificationToOperatorAsync(Guid operatorId, object notification)
    {
        try
        {
            await _hubContext.Clients.User(operatorId.ToString()).SendAsync("Notification", notification);

            _logger.LogDebug("Notification sent to operator {OperatorId}", operatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to operator {OperatorId}", operatorId);
        }
    }
}