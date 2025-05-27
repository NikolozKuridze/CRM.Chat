namespace CRM.Chat.Domain.Entities.Chats.DomainEvents;

public sealed class ChatAbandonedEvent : DomainEvent
{
    public Guid? OperatorId { get; }

    public ChatAbandonedEvent(Guid aggregateId, string aggregateType, Guid? operatorId)
        : base(aggregateId, aggregateType)
    {
        OperatorId = operatorId;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}