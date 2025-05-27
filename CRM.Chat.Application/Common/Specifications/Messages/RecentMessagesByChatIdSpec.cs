using CRM.Chat.Domain.Entities.Messages;

namespace CRM.Chat.Application.Common.Specifications.Messages;

public sealed class RecentMessagesByChatIdSpec : BaseSpecification<ChatMessage>
{
    public RecentMessagesByChatIdSpec(Guid chatId, TimeSpan timeRange)
        : base(m => m.ChatId == chatId && m.CreatedAt >= DateTimeOffset.UtcNow.Subtract(timeRange))
    {
        ApplyOrderByDescending(m => m.CreatedAt);
    }
}