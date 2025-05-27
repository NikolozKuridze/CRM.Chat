namespace CRM.Chat.Domain.Entities.Messages.DomainEvents;

public sealed class MessageReadEvent : DomainEvent
{
    public Guid ChatId { get; }
    public Guid ReadBy { get; }

    public MessageReadEvent(Guid aggregateId, string aggregateType, Guid chatId, Guid readBy)
        : base(aggregateId, aggregateType)
    {
        ChatId = chatId;
        ReadBy = readBy;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}