using CRM.Chat.Domain.Entities.Messages;
using CRM.Chat.Domain.Entities.Messages.Enums;

namespace CRM.Chat.Application.Common.Specifications.Messages;

public sealed class MessagesByTypeSpec : BaseSpecification<ChatMessage>
{
    public MessagesByTypeSpec(MessageType messageType) : base(m => m.Type == messageType)
    {
        ApplyOrderByDescending(m => m.CreatedAt);
    }
}