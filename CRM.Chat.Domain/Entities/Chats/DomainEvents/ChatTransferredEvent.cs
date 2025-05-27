using CRM.Chat.Domain.Common.Events;

namespace CRM.Chat.Domain.Entities.Chats.DomainEvents;

public sealed class ChatTransferredEvent : DomainEvent
{
    public Guid NewOperatorId { get; }
    public Guid? PreviousOperatorId { get; }
    public string Reason { get; }

    public ChatTransferredEvent(Guid aggregateId, string aggregateType, Guid newOperatorId, Guid? previousOperatorId,
        string reason)
        : base(aggregateId, aggregateType)
    {
        NewOperatorId = newOperatorId;
        PreviousOperatorId = previousOperatorId;
        Reason = reason;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}