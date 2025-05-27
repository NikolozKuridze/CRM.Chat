using CRM.Chat.Domain.Entities.Messages;

namespace CRM.Chat.Application.Common.Specifications.Messages;

public sealed class MessagesByChatIdSpec : BaseSpecification<ChatMessage>
{
    public MessagesByChatIdSpec(Guid chatId) : base(m => m.ChatId == chatId)
    {
        ApplyOrderBy(m => m.CreatedAt);
    }
}