using CRM.Chat.Domain.Entities.Messages;

namespace CRM.Chat.Application.Common.Specifications.Messages;

public sealed class MessagesByChatIdPaginatedSpec : BaseSpecification<ChatMessage>
{
    public MessagesByChatIdPaginatedSpec(Guid chatId, int skip, int take) 
        : base(m => m.ChatId == chatId)
    {
        ApplyOrderByDescending(m => m.CreatedAt);
        ApplyPaging(skip, take);
    }
}