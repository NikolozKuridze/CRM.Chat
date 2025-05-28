using CRM.Chat.Domain.Entities.Messages;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Messages.Commands.EditMessage;

public sealed record EditMessageCommand(
    Guid MessageId,
    string NewContent
) : ICommand<Unit>;

public sealed class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId)
            .NotEmpty()
            .WithMessage("Message ID is required.");

        RuleFor(x => x.NewContent)
            .NotEmpty()
            .MaximumLength(4000)
            .WithMessage("Message content is required and must not exceed 4000 characters.");
    }
}

public sealed class EditMessageCommandHandler(
    IRepository<ChatMessage> messageRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<EditMessageCommandHandler> logger) : ICommandHandler<EditMessageCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(EditMessageCommand request, CancellationToken cancellationToken)
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

            // Only sender can edit their own message
            if (message.SenderId != userContext.Id)
            {
                return Result.Failure<Unit>("Only sender can edit their message", "Forbidden");
            }

            // Check if message can be edited (e.g., within time limit)
            var editTimeLimit = TimeSpan.FromMinutes(15);
            if (DateTimeOffset.UtcNow - message.CreatedAt > editTimeLimit)
            {
                return Result.Failure<Unit>("Message can only be edited within 15 minutes", "BadRequest");
            }

            message.EditMessage(request.NewContent);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send real-time notification
            await notificationService.NotifyMessageEditedAsync(
                message.ChatId,
                message.Id,
                request.NewContent,
                cancellationToken);

            logger.LogInformation("Message {MessageId} edited by user {UserId}",
                request.MessageId, userContext.Id);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error editing message {MessageId} by user {UserId}",
                request.MessageId, userContext.Id);
            return Result.Failure<Unit>("Failed to edit message", "InternalServerError");
        }
    }
}