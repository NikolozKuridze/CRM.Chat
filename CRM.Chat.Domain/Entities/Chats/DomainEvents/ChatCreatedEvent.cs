using CRM.Chat.Domain.Common.Events;
using CRM.Chat.Domain.Entities.Chats.Enums;

namespace CRM.Chat.Domain.Entities.Chats.DomainEvents;

public sealed class ChatCreatedEvent : DomainEvent
{
    public ChatType ChatType { get; }
    public Guid InitiatorId { get; }

    public ChatCreatedEvent(Guid aggregateId, string aggregateType, ChatType chatType, Guid initiatorId)
        : base(aggregateId, aggregateType)
    {
        ChatType = chatType;
        InitiatorId = initiatorId;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}