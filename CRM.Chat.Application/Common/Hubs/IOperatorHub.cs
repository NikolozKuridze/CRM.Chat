namespace CRM.Chat.Application.Common.Hubs;

public interface IOperatorHub
{
    Task NotifyOperatorStatusChangedAsync(Guid operatorId, string status);
    Task NotifyNewChatAssignmentAsync(Guid operatorId, Guid chatId);
    Task NotifyChatRemovedAsync(Guid operatorId, Guid chatId);
    Task SendOperatorWorkloadUpdateAsync(Guid operatorId, double workloadPercentage);
    Task BroadcastOperatorAvailabilityAsync(Guid operatorId, bool isAvailable);
    Task SendNotificationToOperatorAsync(Guid operatorId, object notification);
}