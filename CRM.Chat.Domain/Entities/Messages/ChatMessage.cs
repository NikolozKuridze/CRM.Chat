using CRM.Chat.Domain.Entities.Messages.DomainEvents;
using CRM.Chat.Domain.Entities.Messages.Enums;

namespace CRM.Chat.Domain.Entities.Messages;

public class ChatMessage : AggregateRoot
{
    public Guid ChatId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public MessageType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public Guid? ReadBy { get; private set; }
    public bool IsEdited { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }
    public string? OriginalContent { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation properties
    public virtual Chats.Chat Chat { get; private set; } = null!;

    protected ChatMessage()
    {
    }

    public ChatMessage(Guid chatId, Guid senderId, string content, MessageType type = MessageType.Text)
    {
        ChatId = chatId;
        SenderId = senderId;
        Content = content;
        Type = type;
        IsRead = false;

        AddDomainEvent(new MessageSentEvent(Id, GetType().Name, chatId, senderId, content, type));
    }

    public void MarkAsRead(Guid readBy)
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
        ReadBy = readBy;

        AddDomainEvent(new MessageReadEvent(Id, GetType().Name, ChatId, readBy));
    }

    public void EditMessage(string newContent)
    {
        if (Type != MessageType.Text)
            throw new InvalidOperationException("Only text messages can be edited");

        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Message content cannot be empty");

        OriginalContent = Content;
        Content = newContent;
        IsEdited = true;
        EditedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new MessageEditedEvent(Id, GetType().Name, ChatId, newContent, OriginalContent));
    }

    public void UpdateMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}