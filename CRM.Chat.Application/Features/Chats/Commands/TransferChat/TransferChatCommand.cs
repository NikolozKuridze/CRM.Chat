using CRM.Chat.Application.Common.Specifications.Chats;
using CRM.Chat.Application.Common.Specifications.Operators;
using CRM.Chat.Domain.Entities.Operators;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Commands.TransferChat;

public sealed record TransferChatCommand(
    Guid ChatId,
    Guid NewOperatorId,
    string Reason
) : IRequest<Unit>;

public sealed class TransferChatCommandValidator : AbstractValidator<TransferChatCommand>
{
    public TransferChatCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");

        RuleFor(x => x.NewOperatorId)
            .NotEmpty()
            .WithMessage("New operator ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("Transfer reason is required and must not exceed 500 characters.");
    }
}

public sealed class TransferChatCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IRepository<ChatOperator> operatorRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<TransferChatCommandHandler> logger) : IRequestHandler<TransferChatCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(TransferChatCommand request, CancellationToken cancellationToken)
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

            // Verify new operator exists and can accept chats
            var newOperatorSpec = new OperatorByUserIdSpec(request.NewOperatorId);
            var newOperator = await operatorRepository.FirstOrDefaultAsync(newOperatorSpec, cancellationToken);

            if (newOperator == null)
            {
                return Result.Failure<Unit>("Target operator not found", "NotFound");
            }

            if (!newOperator.CanAcceptNewChat())
            {
                return Result.Failure<Unit>("Target operator cannot accept new chats", "BadRequest");
            }

            // Check permissions - only assigned operator or admin can transfer
            var canTransfer = chat.AssignedOperatorId == userContext.Id;

            if (!canTransfer)
            {
                return Result.Failure<Unit>("Insufficient permissions to transfer chat", "Forbidden");
            }

            var previousOperatorId = chat.AssignedOperatorId;

            // Update previous operator's chat count
            if (previousOperatorId.HasValue)
            {
                var previousOperatorSpec = new OperatorByUserIdSpec(previousOperatorId.Value);
                var previousOperator =
                    await operatorRepository.FirstOrDefaultAsync(previousOperatorSpec, cancellationToken);
                previousOperator?.UnassignChat();
            }

            // Transfer chat and update new operator's chat count
            chat.TransferToOperator(request.NewOperatorId, request.Reason);
            newOperator.AssignChat();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notifications
            if (previousOperatorId.HasValue)
            {
                await notificationService.NotifyChatTransferredAsync(
                    chat.Id,
                    previousOperatorId.Value,
                    request.NewOperatorId,
                    request.Reason,
                    cancellationToken);
            }

            logger.LogInformation(
                "Chat {ChatId} transferred from operator {PreviousOperatorId} to {NewOperatorId} by {UserId}. Reason: {Reason}",
                chat.Id, previousOperatorId, request.NewOperatorId, userContext.Id, request.Reason);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transferring chat {ChatId} to operator {NewOperatorId}",
                request.ChatId, request.NewOperatorId);
            return Result.Failure<Unit>("Failed to transfer chat", "InternalServerError");
        }
    }
}