using CRM.Chat.Domain.Entities.Operators.Enums;

namespace CRM.Chat.Domain.Entities.Operators.DomainEvents;

public sealed class OperatorStatusChangedEvent : DomainEvent
{
    public Guid UserId { get; }
    public OperatorStatus NewStatus { get; }

    public OperatorStatusChangedEvent(Guid aggregateId, string aggregateType, Guid userId, OperatorStatus newStatus)
        : base(aggregateId, aggregateType)
    {
        UserId = userId;
        NewStatus = newStatus;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}