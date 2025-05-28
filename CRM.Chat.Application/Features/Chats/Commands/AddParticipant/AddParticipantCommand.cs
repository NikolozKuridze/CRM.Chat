using CRM.Chat.Application.Common.Specifications.Chats;
using CRM.Chat.Domain.Entities.Participants;
using CRM.Chat.Domain.Entities.Participants.Enums;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Commands.AddParticipant;

public sealed record AddParticipantCommand(
    Guid ChatId,
    Guid UserId,
    ParticipantRole Role = ParticipantRole.Customer
) : ICommand<Unit>;

public sealed class AddParticipantCommandValidator : AbstractValidator<AddParticipantCommand>
{
    public AddParticipantCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Valid participant role is required.");
    }
}

public sealed class AddParticipantCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IRepository<ChatParticipant> participantRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<AddParticipantCommandHandler> logger) : ICommandHandler<AddParticipantCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(AddParticipantCommand request, CancellationToken cancellationToken)
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

            // Check if current user has permission to add participants
            var canAddParticipants = chat.InitiatorId == userContext.Id ||
                                     chat.AssignedOperatorId == userContext.Id ||
                                     userContext.IsInRole("Admin");

            if (!canAddParticipants)
            {
                return Result.Failure<Unit>("Insufficient permissions to add participants", "Forbidden");
            }

            // Check if participant already exists
            var existingParticipant = chat.Participants
                .FirstOrDefault(p => p.UserId == request.UserId);

            if (existingParticipant != null)
            {
                if (existingParticipant.IsActive)
                {
                    return Result.Failure<Unit>("User is already a participant in this chat", "Conflict");
                }

                // Reactivate participant
                existingParticipant.RejoinChat();
            }
            else
            {
                // Add new participant
                var participant = new ChatParticipant(request.ChatId, request.UserId, request.Role);
                await participantRepository.AddAsync(participant, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notification
            await notificationService.NotifyParticipantAddedAsync(
                request.ChatId,
                request.UserId,
                userContext.Id,
                cancellationToken);

            logger.LogInformation("Participant {ParticipantId} added to chat {ChatId} by user {UserId}",
                request.UserId, request.ChatId, userContext.Id);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding participant {ParticipantId} to chat {ChatId}",
                request.UserId, request.ChatId);
            return Result.Failure<Unit>("Failed to add participant", "InternalServerError");
        }
    }
}