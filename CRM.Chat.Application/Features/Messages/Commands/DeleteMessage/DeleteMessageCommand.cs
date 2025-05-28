using CRM.Chat.Domain.Entities.Messages;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Messages.Commands.DeleteMessage;

public sealed record DeleteMessageCommand(
    Guid MessageId
) : ICommand<Unit>;

public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("Message ID is required.");
    }
}

public sealed class DeleteMessageCommandHandler(
    IRepository<ChatMessage> messageRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<DeleteMessageCommandHandler> logger) : ICommandHandler<DeleteMessageCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<Unit>("User must be authenticated", "Unauthorized");
            }

            var message = await messageRepository.GetByIdAsync(request.MessageId, cancellationToken);

            if (message == null)
            {
                return Result.Failure<Unit>("Message not found", "NotFound");
            }

            // Only sender or admin can delete message
            if (message.SenderId != userContext.Id && !userContext.IsInRole("Admin"))
            {
                return Result.Failure<Unit>("Insufficient permissions to delete message", "Forbidden");
            }

            // Check if message can be deleted (e.g., within time limit for users)
            if (!userContext.IsInRole("Admin"))
            {
                var deleteTimeLimit = TimeSpan.FromMinutes(5);
                if (DateTimeOffset.UtcNow - message.CreatedAt > deleteTimeLimit)
                {
                    return Result.Failure<Unit>("Message can only be deleted within 5 minutes", "BadRequest");
                }
            }

            // Instead of hard delete, mark as deleted in metadata
            message.UpdateMetadata("IsDeleted", true);
            message.UpdateMetadata("DeletedAt", DateTimeOffset.UtcNow);
            message.UpdateMetadata("DeletedBy", userContext.Id);

            // Update content to show it was deleted
            message.EditMessage("[This message has been deleted]");

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send real-time notification
            await notificationService.NotifyMessageDeletedAsync(
                message.ChatId,
                message.Id,
                cancellationToken);

            logger.LogInformation("Message {MessageId} deleted by user {UserId}",
                request.MessageId, userContext.Id);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting message {MessageId} by user {UserId}",
                request.MessageId, userContext.Id);
            return Result.Failure<Unit>("Failed to delete message", "InternalServerError");
        }
    }
}