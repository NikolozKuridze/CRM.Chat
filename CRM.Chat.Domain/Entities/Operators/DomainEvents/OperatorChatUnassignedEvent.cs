namespace CRM.Chat.Domain.Entities.Operators.DomainEvents;

public sealed class OperatorChatUnassignedEvent : DomainEvent
{
    public Guid UserId { get; }
    public int CurrentChatCount { get; }

    public OperatorChatUnassignedEvent(Guid aggregateId, string aggregateType, Guid userId, int currentChatCount)
        : base(aggregateId, aggregateType)
    {
        UserId = userId;
        CurrentChatCount = currentChatCount;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}