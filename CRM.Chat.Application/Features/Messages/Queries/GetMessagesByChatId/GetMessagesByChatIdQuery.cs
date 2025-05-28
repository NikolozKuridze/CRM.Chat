using CRM.Chat.Application.Common.Specifications.Messages;
using CRM.Chat.Domain.Entities.Messages;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Messages.Queries.GetMessagesByChatId;

public sealed record GetMessagesByChatIdQuery(
    Guid ChatId,
    int PageIndex = 1,
    int PageSize = 50
) : IQuery<PagedResult<MessageDto>>;

public sealed record MessageDto(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    string Content,
    string Type,
    Guid? FileId,
    bool IsRead,
    DateTimeOffset? ReadAt,
    Guid? ReadBy,
    bool IsEdited,
    DateTimeOffset? EditedAt,
    DateTimeOffset CreatedAt);

public sealed record PagedResult<T>(
    IEnumerable<T> Items,
    int PageIndex,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed class GetMessagesByChatIdQueryValidator : AbstractValidator<GetMessagesByChatIdQuery>
{
    public GetMessagesByChatIdQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");

        RuleFor(x => x.PageIndex)
            .GreaterThan(0)
            .WithMessage("Page index must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}

public sealed class GetMessagesByChatIdQueryHandler(
    IRepository<ChatMessage> messageRepository,
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IUserContext userContext,
    ILogger<GetMessagesByChatIdQueryHandler> logger) : IQueryHandler<GetMessagesByChatIdQuery, PagedResult<MessageDto>>
{
    public async ValueTask<Result<PagedResult<MessageDto>>> Handle(GetMessagesByChatIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<PagedResult<MessageDto>>("User must be authenticated", "Unauthorized");
            }

            // Check if chat exists and user has access
            var chat = await chatRepository.GetByIdAsync(request.ChatId, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<PagedResult<MessageDto>>("Chat not found", "NotFound");
            }

            // Verify user has access to this chat
            var hasAccess = chat.InitiatorId == userContext.Id ||
                            chat.AssignedOperatorId == userContext.Id;

            if (!hasAccess)
            {
                return Result.Failure<PagedResult<MessageDto>>("Insufficient permissions to view messages",
                    "Forbidden");
            }

            // Get total count
            var countSpec = new MessagesByChatIdSpec(request.ChatId);
            var totalCount = await messageRepository.CountAsync(countSpec, cancellationToken);

            // Get paginated messages
            var skip = (request.PageIndex - 1) * request.PageSize;
            var paginatedSpec = new MessagesByChatIdPaginatedSpec(request.ChatId, skip, request.PageSize);
            var messages = await messageRepository.ListAsync(paginatedSpec, cancellationToken);

            var messageDtos = messages.Select(m => new MessageDto(
                m.Id,
                m.ChatId,
                m.SenderId,
                m.Content,
                m.Type.ToString(),
                GetFileIdFromMetadata(m.Metadata),
                m.IsRead,
                m.ReadAt,
                m.ReadBy,
                m.IsEdited,
                m.EditedAt,
                m.CreatedAt
            ));

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var pagedResult = new PagedResult<MessageDto>(
                messageDtos,
                request.PageIndex,
                request.PageSize,
                totalCount,
                totalPages,
                request.PageIndex > 1,
                request.PageIndex < totalPages
            );

            logger.LogInformation("Retrieved {MessageCount} messages for chat {ChatId} (page {PageIndex})",
                messages.Count, request.ChatId, request.PageIndex);

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving messages for chat {ChatId}", request.ChatId);
            return Result.Failure<PagedResult<MessageDto>>("Failed to retrieve messages", "InternalServerError");
        }
    }

    private static Guid? GetFileIdFromMetadata(Dictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("FileId", out var fileId) && fileId is string fileIdStr)
        {
            if (Guid.TryParse(fileIdStr, out var parsedFileId))
            {
                return parsedFileId;
            }
        }

        return null;
    }
}