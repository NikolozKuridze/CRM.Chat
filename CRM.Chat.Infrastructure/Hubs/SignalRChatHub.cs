using CRM.Chat.Infrastructure.Hubs.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Hubs;

public class SignalRChatHub : IChatHub
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRChatHub> _logger;

    public SignalRChatHub(IHubContext<ChatHub> hubContext, ILogger<SignalRChatHub> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task JoinChatAsync(Guid chatId, Guid userId)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            var connectionId = GetConnectionIdForUser(userId);
            
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
                _logger.LogInformation("User {UserId} joined chat {ChatId}", userId, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to chat {ChatId}", userId, chatId);
        }
    }

    public async Task LeaveChatAsync(Guid chatId, Guid userId)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            var connectionId = GetConnectionIdForUser(userId);
            
            if (!string.IsNullOrEmpty(connectionId))
            {
                await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
                _logger.LogInformation("User {UserId} left chat {ChatId}", userId, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from chat {ChatId}", userId, chatId);
        }
    }

    public async Task SendMessageToChatAsync(Guid chatId, object message)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);
            
            _logger.LogDebug("Message sent to chat {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to chat {ChatId}", chatId);
        }
    }

    public async Task SendTypingIndicatorAsync(Guid chatId, Guid userId, bool isTyping)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            await _hubContext.Clients.Group(groupName).SendAsync("TypingIndicator", new { UserId = userId, IsTyping = isTyping });
            
            _logger.LogDebug("Typing indicator sent for user {UserId} in chat {ChatId}: {IsTyping}", 
                userId, chatId, isTyping);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator for user {UserId} in chat {ChatId}", 
                userId, chatId);
        }
    }

    public async Task NotifyMessageReadAsync(Guid chatId, Guid messageId, Guid readBy)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            await _hubContext.Clients.Group(groupName).SendAsync("MessageRead", new 
            { 
                MessageId = messageId, 
                ReadBy = readBy, 
                ReadAt = DateTimeOffset.UtcNow 
            });
            
            _logger.LogDebug("Message read notification sent for message {MessageId} in chat {ChatId}", 
                messageId, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message read notification for message {MessageId} in chat {ChatId}", 
                messageId, chatId);
        }
    }

    public async Task NotifyChatStatusChangedAsync(Guid chatId, string status)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ChatStatusChanged", new 
            { 
                ChatId = chatId, 
                Status = status, 
                Timestamp = DateTimeOffset.UtcNow 
            });
            
            _logger.LogInformation("Chat status change notification sent for chat {ChatId}: {Status}", 
                chatId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat status change notification for chat {ChatId}", chatId);
        }
    }

    public async Task NotifyChatAssignedAsync(Guid chatId, Guid operatorId)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ChatAssigned", new 
            { 
                ChatId = chatId, 
                OperatorId = operatorId, 
                Timestamp = DateTimeOffset.UtcNow 
            });
            
            _logger.LogInformation("Chat assignment notification sent for chat {ChatId} to operator {OperatorId}", 
                chatId, operatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat assignment notification for chat {ChatId}", chatId);
        }
    }

    public async Task NotifyChatTransferredAsync(Guid chatId, Guid fromOperatorId, Guid toOperatorId, string reason)
    {
        try
        {
            var groupName = $"chat_{chatId}";
            await _hubContext.Clients.Group(groupName).SendAsync("ChatTransferred", new 
            { 
                ChatId = chatId, 
                FromOperatorId = fromOperatorId, 
                ToOperatorId = toOperatorId, 
                Reason = reason, 
                Timestamp = DateTimeOffset.UtcNow 
            });
            
            _logger.LogInformation("Chat transfer notification sent for chat {ChatId} from {FromOperatorId} to {ToOperatorId}", 
                chatId, fromOperatorId, toOperatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat transfer notification for chat {ChatId}", chatId);
        }
    }

    private string? GetConnectionIdForUser(Guid userId)
    {
        // This is a simplified implementation. In a real-world scenario,
        // you would need to maintain a mapping of user IDs to connection IDs
        // This could be stored in Redis or in-memory cache
        // For now, we'll return null and let the group-based messaging handle it
        return null;
    }
}