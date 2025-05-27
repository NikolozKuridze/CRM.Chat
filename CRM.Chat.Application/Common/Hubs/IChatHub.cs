namespace CRM.Chat.Application.Common.Hubs;

public interface IChatHub
{
    Task JoinChatAsync(Guid chatId, Guid userId);
    Task LeaveChatAsync(Guid chatId, Guid userId);
    Task SendMessageToChatAsync(Guid chatId, object message);
    Task SendTypingIndicatorAsync(Guid chatId, Guid userId, bool isTyping);
    Task NotifyMessageReadAsync(Guid chatId, Guid messageId, Guid readBy);
    Task NotifyChatStatusChangedAsync(Guid chatId, string status);
    Task NotifyChatAssignedAsync(Guid chatId, Guid operatorId);
    Task NotifyChatTransferredAsync(Guid chatId, Guid fromOperatorId, Guid toOperatorId, string reason);
}