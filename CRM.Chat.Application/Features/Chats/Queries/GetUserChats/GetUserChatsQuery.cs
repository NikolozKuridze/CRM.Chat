using CRM.Chat.Application.Common.Specifications.Chats;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Queries.GetUserChats;

public sealed record GetUserChatsQuery(
    Guid? UserId = null,
    int PageIndex = 1,
    int PageSize = 20
) : IRequest<IEnumerable<ChatSummaryDto>>;

public sealed record ChatSummaryDto(
    Guid Id,
    string Title,
    string Type,
    string Status,
    Guid InitiatorId,
    Guid? AssignedOperatorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActivityAt,
    int UnreadCount,
    string? LastMessage);

public sealed class GetUserChatsQueryValidator : AbstractValidator<GetUserChatsQuery>
{
    public GetUserChatsQueryValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThan(0)
            .WithMessage("Page index must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}

public sealed class GetUserChatsQueryHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IUserContext userContext,
    ILogger<GetUserChatsQueryHandler> logger) : IRequestHandler<GetUserChatsQuery, IEnumerable<ChatSummaryDto>>
{
    public async ValueTask<Result<IEnumerable<ChatSummaryDto>>> Handle(GetUserChatsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<IEnumerable<ChatSummaryDto>>("User must be authenticated", "Unauthorized");
            }

            var targetUserId = request.UserId ?? userContext.Id;

            // Regular users can only view their own chats
            if (targetUserId != userContext.Id)
            {
                // TODO: Add role-based authorization check for admins/operators
                return Result.Failure<IEnumerable<ChatSummaryDto>>("Insufficient permissions", "Forbidden");
            }

            var chatSpec = new ChatsByInitiatorIdSpec(targetUserId);
            var chats = await chatRepository.ListAsync(chatSpec, cancellationToken);

            var chatSummaries = chats.Select(chat => new ChatSummaryDto(
                chat.Id,
                chat.Title,
                chat.Type.ToString(),
                chat.Status.ToString(),
                chat.InitiatorId,
                chat.AssignedOperatorId,
                chat.CreatedAt,
                chat.LastActivityAt,
                chat.Messages.Count(m => !m.IsRead && m.SenderId != targetUserId),
                chat.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Content
            ));

            logger.LogInformation("Retrieved {ChatCount} chats for user {UserId}", chats.Count, targetUserId);

            return Result.Success(chatSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving chats for user {UserId}", request.UserId ?? userContext.Id);
            return Result.Failure<IEnumerable<ChatSummaryDto>>("Failed to retrieve chats", "InternalServerError");
        }
    }
}