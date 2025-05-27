using CRM.Chat.Domain.Common.Events;

namespace CRM.Chat.Domain.Entities.Chats.DomainEvents;

public sealed class ChatClosedEvent : DomainEvent
{
    public Guid? OperatorId { get; }
    public string? Reason { get; }

    public ChatClosedEvent(Guid aggregateId, string aggregateType, Guid? operatorId, string? reason)
        : base(aggregateId, aggregateType)
    {
        OperatorId = operatorId;
        Reason = reason;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}