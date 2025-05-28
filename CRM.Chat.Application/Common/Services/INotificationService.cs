using CRM.Chat.Domain.Common.Models;

namespace CRM.Chat.Application.Common.Services;

public interface INotificationService
{
    Task<Result<Unit>> NotifyNewChatAsync(Guid chatId, Guid operatorId, CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyNewMessageAsync(Guid chatId, Guid messageId, Guid senderId,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyMessageReadAsync(Guid chatId, Guid messageId, Guid readBy,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyChatClosedAsync(Guid chatId, string? reason,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyChatTransferredAsync(Guid chatId, Guid fromOperatorId, Guid toOperatorId, string reason,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyOperatorStatusChangedAsync(Guid operatorId, string status,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyTypingAsync(Guid chatId, Guid userId, bool isTyping,
        CancellationToken cancellationToken = default);
 
    Task<Result<Unit>> NotifyMessageEditedAsync(Guid chatId, Guid messageId, string newContent,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyMessageDeletedAsync(Guid chatId, Guid messageId,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyParticipantAddedAsync(Guid chatId, Guid participantId, Guid addedBy,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> NotifyParticipantRemovedAsync(Guid chatId, Guid participantId, Guid removedBy,
        CancellationToken cancellationToken = default);
}