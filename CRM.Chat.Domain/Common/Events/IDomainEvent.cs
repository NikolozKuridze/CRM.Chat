namespace CRM.Chat.Domain.Common.Events;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
    Guid AggregateId { get; }
    string AggregateType { get; }
    ProcessingStrategy ProcessingStrategy { get; set; }
}

public enum ProcessingStrategy
{
    Immediate = 0,
    Background = 1
}