using CRM.Chat.Application.Features.Chats.Queries.GetUserChats;
using CRM.Chat.Application.Features.Messages.Queries.GetMessagesByChatId;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Queries.SearchChats;

public sealed record SearchChatsQuery(
    string? SearchTerm = null,
    ChatType? Type = null,
    ChatStatus? Status = null,
    Guid? OperatorId = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int PageIndex = 1,
    int PageSize = 20
) : IQuery<PagedResult<ChatSummaryDto>>;

public sealed class SearchChatsQueryValidator : AbstractValidator<SearchChatsQuery>
{
    public SearchChatsQueryValidator()
    {
        RuleFor(x => x.PageIndex)
            .GreaterThan(0)
            .WithMessage("Page index must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.StartDate!.Value <= x.EndDate!.Value)
                .WithMessage("Start date must be before or equal to end date.");
        });
    }
}

public sealed class SearchChatsQueryHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IUserContext userContext,
    ILogger<SearchChatsQueryHandler> logger) : IQueryHandler<SearchChatsQuery, PagedResult<ChatSummaryDto>>
{
    public async ValueTask<Result<PagedResult<ChatSummaryDto>>> Handle(SearchChatsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<PagedResult<ChatSummaryDto>>("User must be authenticated", "Unauthorized");
            }

            // Build query
            var query = chatRepository.GetQueryable();

            // Apply filters based on user role
            if (!userContext.IsInRole("Admin"))
            {
                // Non-admin users can only see their own chats
                query = query.Where(c => c.InitiatorId == userContext.Id ||
                                         c.AssignedOperatorId == userContext.Id ||
                                         c.Participants.Any(p => p.UserId == userContext.Id));
            }

            // Search term filter (title or description)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTermLower = request.SearchTerm.ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(searchTermLower) ||
                                         (c.Description != null && c.Description.ToLower().Contains(searchTermLower)));
            }

            // Type filter
            if (request.Type.HasValue)
            {
                query = query.Where(c => c.Type == request.Type.Value);
            }

            // Status filter
            if (request.Status.HasValue)
            {
                query = query.Where(c => c.Status == request.Status.Value);
            }

            // Operator filter
            if (request.OperatorId.HasValue)
            {
                query = query.Where(c => c.AssignedOperatorId == request.OperatorId.Value);
            }

            // Date range filter
            if (request.StartDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(c => c.CreatedAt <= request.EndDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting and pagination
            var skip = (request.PageIndex - 1) * request.PageSize;
            var paginatedChats = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(request.PageSize)
                .Include(c => c.Messages)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var chatSummaries = paginatedChats.Select(chat => new ChatSummaryDto(
                chat.Id,
                chat.Title,
                chat.Type.ToString(),
                chat.Status.ToString(),
                chat.InitiatorId,
                chat.AssignedOperatorId,
                chat.CreatedAt,
                chat.LastActivityAt,
                chat.Messages.Count(m => !m.IsRead && m.SenderId != userContext.Id),
                chat.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Content
            ));

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var pagedResult = new PagedResult<ChatSummaryDto>(
                chatSummaries,
                request.PageIndex,
                request.PageSize,
                totalCount,
                totalPages,
                request.PageIndex > 1,
                request.PageIndex < totalPages
            );

            logger.LogInformation("Search returned {ChatCount} chats (page {PageIndex} of {TotalPages})",
                paginatedChats.Count, request.PageIndex, totalPages);

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching chats with filters");
            return Result.Failure<PagedResult<ChatSummaryDto>>("Failed to search chats", "InternalServerError");
        }
    }
}