using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Commands.SendTypingIndicator;

public sealed record SendTypingIndicatorCommand(
    Guid ChatId,
    bool IsTyping
) : ICommand<Unit>;

public sealed class SendTypingIndicatorCommandValidator : AbstractValidator<SendTypingIndicatorCommand>
{
    public SendTypingIndicatorCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");
    }
}

public sealed class SendTypingIndicatorCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    INotificationService notificationService,
    IUserContext userContext,
    ILogger<SendTypingIndicatorCommandHandler> logger) : ICommandHandler<SendTypingIndicatorCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(SendTypingIndicatorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<Unit>("User must be authenticated", "Unauthorized");
            }

            var chat = await chatRepository.GetByIdAsync(request.ChatId, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Unit>("Chat not found", "NotFound");
            }

            // Check if user is participant in this chat
            var isParticipant = chat.InitiatorId == userContext.Id ||
                                chat.AssignedOperatorId == userContext.Id;

            if (!isParticipant)
            {
                return Result.Failure<Unit>("User is not a participant in this chat", "Forbidden");
            }

            // Send typing notification through SignalR
            await notificationService.NotifyTypingAsync(
                request.ChatId,
                userContext.Id,
                request.IsTyping,
                cancellationToken);

            logger.LogDebug("Typing indicator sent for user {UserId} in chat {ChatId}: {IsTyping}",
                userContext.Id, request.ChatId, request.IsTyping);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending typing indicator for user {UserId} in chat {ChatId}",
                userContext.Id, request.ChatId);
            return Result.Failure<Unit>("Failed to send typing indicator", "InternalServerError");
        }
    }
}