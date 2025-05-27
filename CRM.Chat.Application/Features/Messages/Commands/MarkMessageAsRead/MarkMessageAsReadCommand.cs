using CRM.Chat.Domain.Entities.Messages;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Messages.Commands.MarkMessageAsRead;

public sealed record MarkMessageAsReadCommand(
    Guid MessageId
) : IRequest<Unit>;

public sealed class MarkMessageAsReadCommandValidator : AbstractValidator<MarkMessageAsReadCommand>
{
    public MarkMessageAsReadCommandValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("Message ID is required.");
    }
}

public sealed class MarkMessageAsReadCommandHandler(
    IRepository<ChatMessage> messageRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<MarkMessageAsReadCommandHandler> logger) : IRequestHandler<MarkMessageAsReadCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
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

            // Users can only mark messages as read if they are not the sender
            if (message.SenderId == userContext.Id)
            {
                return Result.Failure<Unit>("Cannot mark own message as read", "BadRequest");
            }

            if (message.IsRead)
            {
                return Result.Success(Unit.Value); // Already read
            }

            message.MarkAsRead(userContext.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send real-time notification
            await notificationService.NotifyMessageReadAsync(
                message.ChatId,
                message.Id,
                userContext.Id,
                cancellationToken);

            logger.LogInformation("Message {MessageId} marked as read by user {UserId}",
                request.MessageId, userContext.Id);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error marking message {MessageId} as read by user {UserId}",
                request.MessageId, userContext.Id);
            return Result.Failure<Unit>("Failed to mark message as read", "InternalServerError");
        }
    }
}