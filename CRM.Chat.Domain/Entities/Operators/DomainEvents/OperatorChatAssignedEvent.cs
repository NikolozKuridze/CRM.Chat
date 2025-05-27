namespace CRM.Chat.Domain.Entities.Operators.DomainEvents;

public sealed class OperatorChatAssignedEvent : DomainEvent
{
    public Guid UserId { get; }
    public int CurrentChatCount { get; }

    public OperatorChatAssignedEvent(Guid aggregateId, string aggregateType, Guid userId, int currentChatCount)
        : base(aggregateId, aggregateType)
    {
        UserId = userId;
        CurrentChatCount = currentChatCount;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}