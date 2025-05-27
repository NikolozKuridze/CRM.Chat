using CRM.Chat.Domain.Entities.Messages.Enums;

namespace CRM.Chat.Domain.Entities.Messages.DomainEvents;

public sealed class MessageSentEvent : DomainEvent
{
    public Guid ChatId { get; }
    public Guid SenderId { get; }
    public string Content { get; }
    public MessageType MessageType { get; }

    public MessageSentEvent(Guid aggregateId, string aggregateType, Guid chatId, Guid senderId, string content, MessageType messageType)
        : base(aggregateId, aggregateType)
    {
        ChatId = chatId;
        SenderId = senderId;
        Content = content;
        MessageType = messageType;
        ProcessingStrategy = ProcessingStrategy.Immediate;
    }
}