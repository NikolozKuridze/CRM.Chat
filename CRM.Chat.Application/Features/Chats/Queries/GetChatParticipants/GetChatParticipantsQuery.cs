using CRM.Chat.Application.Common.Specifications.Chats;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Queries.GetChatParticipants;

public sealed record GetChatParticipantsQuery(
    Guid ChatId
) : IQuery<IEnumerable<ParticipantDto>>;

public sealed record ParticipantDto(
    Guid UserId,
    string Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt,
    bool IsActive,
    DateTimeOffset? LastSeenAt,
    int UnreadCount);

public sealed class GetChatParticipantsQueryValidator : AbstractValidator<GetChatParticipantsQuery>
{
    public GetChatParticipantsQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");
    }
}

public sealed class GetChatParticipantsQueryHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IUserContext userContext,
    ILogger<GetChatParticipantsQueryHandler> logger)
    : IQueryHandler<GetChatParticipantsQuery, IEnumerable<ParticipantDto>>
{
    public async ValueTask<Result<IEnumerable<ParticipantDto>>> Handle(GetChatParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<IEnumerable<ParticipantDto>>("User must be authenticated", "Unauthorized");
            }

            var chatSpec = new ChatByIdWithMessagesSpec(request.ChatId);
            var chat = await chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<IEnumerable<ParticipantDto>>("Chat not found", "NotFound");
            }

            // Check if user has access to this chat
            var hasAccess = chat.InitiatorId == userContext.Id ||
                            chat.AssignedOperatorId == userContext.Id ||
                            chat.Participants.Any(p => p.UserId == userContext.Id);

            if (!hasAccess)
            {
                return Result.Failure<IEnumerable<ParticipantDto>>("Insufficient permissions to view participants",
                    "Forbidden");
            }

            var participants = new List<ParticipantDto>();

            // Add initiator as implicit participant
            participants.Add(new ParticipantDto(
                chat.InitiatorId,
                "Customer",
                chat.CreatedAt,
                null,
                true,
                chat.LastActivityAt,
                0));

            // Add assigned operator if exists
            if (chat.AssignedOperatorId.HasValue)
            {
                participants.Add(new ParticipantDto(
                    chat.AssignedOperatorId.Value,
                    "Operator",
                    chat.CreatedAt,
                    null,
                    true,
                    chat.LastActivityAt,
                    0));
            }

            // Add explicit participants
            var explicitParticipants = chat.Participants.Select(p => new ParticipantDto(
                p.UserId,
                p.Role.ToString(),
                p.JoinedAt,
                p.LeftAt,
                p.IsActive,
                p.LastSeenAt,
                p.UnreadCount));

            participants.AddRange(explicitParticipants);

            // Remove duplicates (in case someone is both initiator/operator and explicit participant)
            var uniqueParticipants = participants
                .GroupBy(p => p.UserId)
                .Select(g => g.First())
                .OrderBy(p => p.JoinedAt);

            logger.LogInformation("Retrieved {ParticipantCount} participants for chat {ChatId}",
                uniqueParticipants.Count(), request.ChatId);

            return Result.Success<IEnumerable<ParticipantDto>>(uniqueParticipants);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving participants for chat {ChatId}", request.ChatId);
            return Result.Failure<IEnumerable<ParticipantDto>>("Failed to retrieve participants",
                "InternalServerError");
        }
    }
}