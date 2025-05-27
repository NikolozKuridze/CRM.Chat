using CRM.Chat.Application.Common.Specifications.Chats;
using CRM.Chat.Application.Common.Specifications.Operators;
using CRM.Chat.Domain.Entities.Operators;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Commands.CloseChat;

public sealed record CloseChatCommand(
    Guid ChatId,
    string? Reason = null
) : IRequest<Unit>;

public sealed class CloseChatCommandValidator : AbstractValidator<CloseChatCommand>
{
    public CloseChatCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("Reason must not exceed 500 characters.");
    }
}

public sealed class CloseChatCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IRepository<ChatOperator> operatorRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<CloseChatCommandHandler> logger) : IRequestHandler<CloseChatCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(CloseChatCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<Unit>("User must be authenticated", "Unauthorized");
            }

            var chatSpec = new ChatByIdWithMessagesSpec(request.ChatId);
            var chat = await chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Unit>("Chat not found", "NotFound");
            }

            // Check permissions - only the assigned operator, chat initiator, or admin can close
            var canClose = chat.InitiatorId == userContext.Id ||
                           chat.AssignedOperatorId == userContext.Id;

            if (!canClose)
            {
                return Result.Failure<Unit>("Insufficient permissions to close chat", "Forbidden");
            }

            chat.CloseChat(request.Reason);

            // Update operator's chat count if assigned
            if (chat.AssignedOperatorId.HasValue)
            {
                var operatorSpec = new OperatorByUserIdSpec(chat.AssignedOperatorId.Value);
                var chatOperator = await operatorRepository.FirstOrDefaultAsync(operatorSpec, cancellationToken);
                chatOperator?.UnassignChat();
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notifications
            await notificationService.NotifyChatClosedAsync(chat.Id, request.Reason, cancellationToken);

            logger.LogInformation("Chat {ChatId} closed by user {UserId} with reason: {Reason}",
                chat.Id, userContext.Id, request.Reason ?? "No reason provided");

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing chat {ChatId}", request.ChatId);
            return Result.Failure<Unit>("Failed to close chat", "InternalServerError");
        }
    }
}