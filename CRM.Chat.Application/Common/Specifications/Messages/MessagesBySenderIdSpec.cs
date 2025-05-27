using CRM.Chat.Domain.Entities.Messages;

namespace CRM.Chat.Application.Common.Specifications.Messages;

public sealed class MessagesBySenderIdSpec : BaseSpecification<ChatMessage>
{
    public MessagesBySenderIdSpec(Guid senderId) : base(m => m.SenderId == senderId)
    {
        ApplyOrderByDescending(m => m.CreatedAt);
    }
}