using CRM.Chat.Domain.Common.Events;

namespace CRM.Chat.Domain.Entities.Chats.DomainEvents;

public sealed class ChatAssignedEvent : DomainEvent
{
    public Guid OperatorId { get; }
    public Guid? PreviousOperatorId { get; }

    public ChatAssignedEvent(Guid aggregateId, string aggregateType, Guid operatorId, Guid? previousOperatorId)
        : base(aggregateId, aggregateType)
    {
        OperatorId = operatorId;
        PreviousOperatorId = previousOperatorId;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}