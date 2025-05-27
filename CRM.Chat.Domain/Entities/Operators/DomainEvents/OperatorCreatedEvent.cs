namespace CRM.Chat.Domain.Entities.Operators.DomainEvents;

public sealed class OperatorCreatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string DisplayName { get; }

    public OperatorCreatedEvent(Guid aggregateId, string aggregateType, Guid userId, string displayName)
        : base(aggregateId, aggregateType)
    {
        UserId = userId;
        DisplayName = displayName;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}