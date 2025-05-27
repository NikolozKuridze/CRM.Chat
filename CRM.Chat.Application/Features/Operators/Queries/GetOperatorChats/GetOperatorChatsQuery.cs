using CRM.Chat.Application.Common.Specifications.Chats;
using CRM.Chat.Application.Features.Chats.Queries.GetUserChats;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Operators.Queries.GetOperatorChats;

public sealed record GetOperatorChatsQuery(
    Guid? OperatorId = null,
    bool ActiveOnly = false
) : IRequest<IEnumerable<ChatSummaryDto>>;

public sealed class GetOperatorChatsQueryValidator : AbstractValidator<GetOperatorChatsQuery>
{
    public GetOperatorChatsQueryValidator()
    {
        // No specific validation needed for now
    }
}

public sealed class GetOperatorChatsQueryHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IUserContext userContext,
    ILogger<GetOperatorChatsQueryHandler> logger) : IRequestHandler<GetOperatorChatsQuery, IEnumerable<ChatSummaryDto>>
{
    public async ValueTask<Result<IEnumerable<ChatSummaryDto>>> Handle(GetOperatorChatsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<IEnumerable<ChatSummaryDto>>("User must be authenticated", "Unauthorized");
            }

            var targetOperatorId = request.OperatorId ?? userContext.Id;

            // Regular users can only view their own assigned chats
            if (targetOperatorId != userContext.Id)
            {
                // TODO: Add role-based authorization check for admins
                return Result.Failure<IEnumerable<ChatSummaryDto>>("Insufficient permissions", "Forbidden");
            }

            ISpecification<Domain.Entities.Chats.Chat> chatSpec = request.ActiveOnly
                ? new ActiveChatsByOperatorIdSpec(targetOperatorId)
                : new ChatsByOperatorIdSpec(targetOperatorId);

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
                chat.Messages.Count(m => !m.IsRead && m.SenderId != targetOperatorId),
                chat.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Content
            ));

            logger.LogInformation("Retrieved {ChatCount} chats for operator {OperatorId}", chats.Count,
                targetOperatorId);

            return Result.Success(chatSummaries);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving chats for operator {OperatorId}",
                request.OperatorId ?? userContext.Id);
            return Result.Failure<IEnumerable<ChatSummaryDto>>("Failed to retrieve operator chats",
                "InternalServerError");
        }
    }
}