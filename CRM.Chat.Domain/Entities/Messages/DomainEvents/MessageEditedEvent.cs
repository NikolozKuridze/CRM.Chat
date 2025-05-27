namespace CRM.Chat.Domain.Entities.Messages.DomainEvents;

public sealed class MessageEditedEvent : DomainEvent
{
    public Guid ChatId { get; }
    public string NewContent { get; }
    public string? OriginalContent { get; }

    public MessageEditedEvent(Guid aggregateId, string aggregateType, Guid chatId, string newContent, string? originalContent)
        : base(aggregateId, aggregateType)
    {
        ChatId = chatId;
        NewContent = newContent;
        OriginalContent = originalContent;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}