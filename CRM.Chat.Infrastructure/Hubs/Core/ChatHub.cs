using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Hubs.Core;

[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} connected to ChatHub with connection {ConnectionId}",
            userId, Context.ConnectionId);

        // Add to general users group
        await Groups.AddToGroupAsync(Context.ConnectionId, "Users");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} disconnected from ChatHub. Connection {ConnectionId}",
            userId, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected due to error", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(string chatId)
    {
        if (Guid.TryParse(chatId, out var parsedChatId))
        {
            var groupName = $"chat_{parsedChatId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var userId = GetUserId();
            _logger.LogInformation("User {UserId} joined chat {ChatId}", userId, parsedChatId);

            // Notify others in the chat that user joined
            await Clients.Group(groupName).SendAsync("UserJoinedChat", new { UserId = userId, ChatId = parsedChatId });
        }
    }

    public async Task LeaveChat(string chatId)
    {
        if (Guid.TryParse(chatId, out var parsedChatId))
        {
            var groupName = $"chat_{parsedChatId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            var userId = GetUserId();
            _logger.LogInformation("User {UserId} left chat {ChatId}", userId, parsedChatId);

            // Notify others in the chat that user left
            await Clients.Group(groupName).SendAsync("UserLeftChat", new { UserId = userId, ChatId = parsedChatId });
        }
    }

    public async Task SendTyping(string chatId, bool isTyping)
    {
        if (Guid.TryParse(chatId, out var parsedChatId))
        {
            var groupName = $"chat_{parsedChatId}";
            var userId = GetUserId();

            // Send typing indicator to others in the chat (excluding sender)
            await Clients.GroupExcept(groupName, Context.ConnectionId)
                .SendAsync("TypingIndicator", new { UserId = userId, ChatId = parsedChatId, IsTyping = isTyping });

            _logger.LogDebug("User {UserId} typing indicator sent to chat {ChatId}: {IsTyping}",
                userId, parsedChatId, isTyping);
        }
    }

    public async Task MarkMessageAsRead(string messageId)
    {
        if (Guid.TryParse(messageId, out var parsedMessageId))
        {
            var userId = GetUserId();

            // This would typically trigger a command through the mediator
            // For now, we'll just log it
            _logger.LogInformation("User {UserId} marked message {MessageId} as read", userId, parsedMessageId);
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.Claims
            .FirstOrDefault(c => c.Type == "Uid" || c.Type == "sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}