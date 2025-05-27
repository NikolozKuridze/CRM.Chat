namespace CRM.Chat.Domain.Common.Events;

public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }
    public string EventType { get; }
    public Guid AggregateId { get; }
    public string AggregateType { get; }
    public ProcessingStrategy ProcessingStrategy { get; set; }

    protected DomainEvent(Guid aggregateId, string aggregateType)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
        EventType = GetType().Name;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        ProcessingStrategy = ProcessingStrategy.Background;
    }
}