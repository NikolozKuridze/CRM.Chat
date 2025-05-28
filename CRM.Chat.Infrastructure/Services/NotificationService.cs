using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IChatHub _chatHub;
    private readonly IOperatorHub _operatorHub;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IChatHub chatHub,
        IOperatorHub operatorHub,
        ILogger<NotificationService> logger)
    {
        _chatHub = chatHub;
        _operatorHub = operatorHub;
        _logger = logger;
    }

    public async Task<Result<Unit>> NotifyNewChatAsync(Guid chatId, Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _operatorHub.NotifyNewChatAssignmentAsync(operatorId, chatId);
            await _chatHub.NotifyChatAssignedAsync(chatId, operatorId);

            _logger.LogInformation("Notified new chat assignment: Chat {ChatId} to Operator {OperatorId}", chatId,
                operatorId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying new chat assignment for chat {ChatId} and operator {OperatorId}",
                chatId, operatorId);
            return Result.Failure<Unit>("Failed to send new chat notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyNewMessageAsync(Guid chatId, Guid messageId, Guid senderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messageNotification = new
            {
                ChatId = chatId,
                MessageId = messageId,
                SenderId = senderId,
                Timestamp = DateTimeOffset.UtcNow
            };

            await _chatHub.SendMessageToChatAsync(chatId, messageNotification);

            _logger.LogInformation("Notified new message: Message {MessageId} in Chat {ChatId} from {SenderId}",
                messageId, chatId, senderId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying new message {MessageId} in chat {ChatId}", messageId, chatId);
            return Result.Failure<Unit>("Failed to send message notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyMessageReadAsync(Guid chatId, Guid messageId, Guid readBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _chatHub.NotifyMessageReadAsync(chatId, messageId, readBy);

            _logger.LogInformation("Notified message read: Message {MessageId} in Chat {ChatId} read by {ReadBy}",
                messageId, chatId, readBy);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying message read for message {MessageId} in chat {ChatId}",
                messageId, chatId);
            return Result.Failure<Unit>("Failed to send read notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyChatClosedAsync(Guid chatId, string? reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _chatHub.NotifyChatStatusChangedAsync(chatId, "Closed");

            _logger.LogInformation("Notified chat closed: Chat {ChatId}, Reason: {Reason}",
                chatId, reason ?? "No reason provided");

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying chat closed for chat {ChatId}", chatId);
            return Result.Failure<Unit>("Failed to send chat closed notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyChatTransferredAsync(Guid chatId, Guid fromOperatorId, Guid toOperatorId,
        string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            await _chatHub.NotifyChatTransferredAsync(chatId, fromOperatorId, toOperatorId, reason);
            await _operatorHub.NotifyChatRemovedAsync(fromOperatorId, chatId);
            await _operatorHub.NotifyNewChatAssignmentAsync(toOperatorId, chatId);

            _logger.LogInformation(
                "Notified chat transfer: Chat {ChatId} from Operator {FromOperatorId} to {ToOperatorId}, Reason: {Reason}",
                chatId, fromOperatorId, toOperatorId, reason);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying chat transfer for chat {ChatId}", chatId);
            return Result.Failure<Unit>("Failed to send transfer notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyOperatorStatusChangedAsync(Guid operatorId, string status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _operatorHub.NotifyOperatorStatusChangedAsync(operatorId, status);

            _logger.LogInformation("Notified operator status change: Operator {OperatorId} status changed to {Status}",
                operatorId, status);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying operator status change for operator {OperatorId}", operatorId);
            return Result.Failure<Unit>("Failed to send status change notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyTypingAsync(Guid chatId, Guid userId, bool isTyping,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _chatHub.SendTypingIndicatorAsync(chatId, userId, isTyping);

            _logger.LogDebug("Notified typing indicator: User {UserId} in Chat {ChatId} is typing: {IsTyping}",
                userId, chatId, isTyping);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying typing indicator for user {UserId} in chat {ChatId}", userId, chatId);
            return Result.Failure<Unit>("Failed to send typing notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyMessageEditedAsync(Guid chatId, Guid messageId, string newContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var editNotification = new
            {
                ChatId = chatId,
                MessageId = messageId,
                NewContent = newContent,
                EditedAt = DateTimeOffset.UtcNow
            };

            await _chatHub.SendMessageToChatAsync(chatId, editNotification);

            _logger.LogInformation("Notified message edited: Message {MessageId} in Chat {ChatId}",
                messageId, chatId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying message edited for message {MessageId} in chat {ChatId}",
                messageId, chatId);
            return Result.Failure<Unit>("Failed to send edit notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyMessageDeletedAsync(Guid chatId, Guid messageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteNotification = new
            {
                ChatId = chatId,
                MessageId = messageId,
                DeletedAt = DateTimeOffset.UtcNow
            };

            await _chatHub.SendMessageToChatAsync(chatId, deleteNotification);

            _logger.LogInformation("Notified message deleted: Message {MessageId} in Chat {ChatId}",
                messageId, chatId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying message deleted for message {MessageId} in chat {ChatId}",
                messageId, chatId);
            return Result.Failure<Unit>("Failed to send delete notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyParticipantAddedAsync(Guid chatId, Guid participantId, Guid addedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var participantNotification = new
            {
                ChatId = chatId,
                ParticipantId = participantId,
                AddedBy = addedBy,
                AddedAt = DateTimeOffset.UtcNow
            };

            await _chatHub.SendMessageToChatAsync(chatId, participantNotification);

            _logger.LogInformation(
                "Notified participant added: User {ParticipantId} added to Chat {ChatId} by {AddedBy}",
                participantId, chatId, addedBy);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying participant added for chat {ChatId}", chatId);
            return Result.Failure<Unit>("Failed to send participant added notification", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> NotifyParticipantRemovedAsync(Guid chatId, Guid participantId, Guid removedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var participantNotification = new
            {
                ChatId = chatId,
                ParticipantId = participantId,
                RemovedBy = removedBy,
                RemovedAt = DateTimeOffset.UtcNow
            };

            await _chatHub.SendMessageToChatAsync(chatId, participantNotification);

            _logger.LogInformation(
                "Notified participant removed: User {ParticipantId} removed from Chat {ChatId} by {RemovedBy}",
                participantId, chatId, removedBy);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying participant removed for chat {ChatId}", chatId);
            return Result.Failure<Unit>("Failed to send participant removed notification", "InternalServerError");
        }
    }
}