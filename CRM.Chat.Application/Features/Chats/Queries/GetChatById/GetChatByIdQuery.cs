using CRM.Chat.Application.Common.Specifications.Chats;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Queries.GetChatById;

public sealed record GetChatByIdQuery(Guid ChatId) : IRequest<ChatDetailsDto>;

public sealed record ChatDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    string Type,
    string Status,
    Guid InitiatorId,
    Guid? AssignedOperatorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActivityAt,
    DateTimeOffset? ClosedAt,
    string? CloseReason,
    int Priority,
    IEnumerable<ChatParticipantDto> Participants,
    IEnumerable<ChatMessageDto> Messages);

public sealed record ChatParticipantDto(
    Guid UserId,
    string Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LeftAt,
    bool IsActive);

public sealed record ChatMessageDto(
    Guid Id,
    Guid SenderId,
    string Content,
    string Type,
    bool IsRead,
    DateTimeOffset? ReadAt,
    Guid? ReadBy,
    bool IsEdited,
    DateTimeOffset CreatedAt);

public sealed class GetChatByIdQueryValidator : AbstractValidator<GetChatByIdQuery>
{
    public GetChatByIdQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");
    }
}

public sealed class GetChatByIdQueryHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IUserContext userContext,
    ILogger<GetChatByIdQueryHandler> logger) : IRequestHandler<GetChatByIdQuery, ChatDetailsDto>
{
    public async ValueTask<Result<ChatDetailsDto>> Handle(GetChatByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<ChatDetailsDto>("User must be authenticated", "Unauthorized");
            }

            var chatSpec = new ChatByIdWithMessagesSpec(request.ChatId);
            var chat = await chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<ChatDetailsDto>("Chat not found", "NotFound");
            }

            // Check if user has access to this chat
            var hasAccess = chat.InitiatorId == userContext.Id ||
                            chat.AssignedOperatorId == userContext.Id ||
                            chat.Participants.Any(p => p.UserId == userContext.Id);

            if (!hasAccess)
            {
                return Result.Failure<ChatDetailsDto>("Insufficient permissions to view chat", "Forbidden");
            }

            var chatDto = new ChatDetailsDto(
                chat.Id,
                chat.Title,
                chat.Description,
                chat.Type.ToString(),
                chat.Status.ToString(),
                chat.InitiatorId,
                chat.AssignedOperatorId,
                chat.CreatedAt,
                chat.LastActivityAt,
                chat.ClosedAt,
                chat.CloseReason,
                chat.Priority,
                chat.Participants.Select(p => new ChatParticipantDto(
                    p.UserId,
                    p.Role.ToString(),
                    p.JoinedAt,
                    p.LeftAt,
                    p.IsActive)),
                chat.Messages.Select(m => new ChatMessageDto(
                        m.Id,
                        m.SenderId,
                        m.Content,
                        m.Type.ToString(),
                        m.IsRead,
                        m.ReadAt,
                        m.ReadBy,
                        m.IsEdited,
                        m.CreatedAt))
                    .OrderBy(m => m.CreatedAt));

            logger.LogInformation("Chat {ChatId} retrieved by user {UserId}", request.ChatId, userContext.Id);

            return Result.Success(chatDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving chat {ChatId} for user {UserId}",
                request.ChatId, userContext.Id);
            return Result.Failure<ChatDetailsDto>("Failed to retrieve chat", "InternalServerError");
        }
    }
}