using CRM.Chat.Application.Common.Specifications.Chats;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Commands.RemoveParticipant;

public sealed record RemoveParticipantCommand(
    Guid ChatId,
    Guid UserId
) : ICommand<Unit>;

public sealed class RemoveParticipantCommandValidator : AbstractValidator<RemoveParticipantCommand>
{
    public RemoveParticipantCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}

public sealed class RemoveParticipantCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<RemoveParticipantCommandHandler> logger) : ICommandHandler<RemoveParticipantCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(RemoveParticipantCommand request, CancellationToken cancellationToken)
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

            // Check if current user has permission to remove participants
            var canRemoveParticipants = chat.InitiatorId == userContext.Id ||
                                        chat.AssignedOperatorId == userContext.Id ||
                                        userContext.IsInRole("Admin") ||
                                        request.UserId == userContext.Id; // User can remove themselves

            if (!canRemoveParticipants)
            {
                return Result.Failure<Unit>("Insufficient permissions to remove participants", "Forbidden");
            }

            // Find participant
            var participant = chat.Participants
                .FirstOrDefault(p => p.UserId == request.UserId && p.IsActive);

            if (participant == null)
            {
                return Result.Failure<Unit>("Participant not found in chat", "NotFound");
            }

            // Don't allow removing chat initiator
            if (request.UserId == chat.InitiatorId)
            {
                return Result.Failure<Unit>("Cannot remove chat initiator", "BadRequest");
            }

            // Leave chat (soft delete)
            participant.LeaveChat();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notification
            await notificationService.NotifyParticipantRemovedAsync(
                request.ChatId,
                request.UserId,
                userContext.Id,
                cancellationToken);

            logger.LogInformation("Participant {ParticipantId} removed from chat {ChatId} by user {UserId}",
                request.UserId, request.ChatId, userContext.Id);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing participant {ParticipantId} from chat {ChatId}",
                request.UserId, request.ChatId);
            return Result.Failure<Unit>("Failed to remove participant", "InternalServerError");
        }
    }
}