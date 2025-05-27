using CRM.Chat.Domain.Entities.Messages;

namespace CRM.Chat.Application.Common.Specifications.Messages;

public sealed class UnreadMessagesByChatIdSpec : BaseSpecification<ChatMessage>
{
    public UnreadMessagesByChatIdSpec(Guid chatId, Guid userId) 
        : base(m => m.ChatId == chatId && !m.IsRead && m.SenderId != userId)
    {
        ApplyOrderBy(m => m.CreatedAt);
    }
}