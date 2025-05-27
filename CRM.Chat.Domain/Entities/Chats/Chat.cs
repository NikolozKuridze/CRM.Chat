using CRM.Chat.Domain.Entities.Chats.DomainEvents;
using CRM.Chat.Domain.Entities.Chats.Enums;

namespace CRM.Chat.Domain.Entities.Chats;

public class Chat : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ChatType Type { get; private set; }
    public ChatStatus Status { get; private set; }
    public Guid InitiatorId { get; private set; }
    public Guid? AssignedOperatorId { get; private set; }
    public DateTimeOffset? LastActivityAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? CloseReason { get; private set; }
    public int Priority { get; private set; } = 1;
    public Dictionary<string, object> Metadata { get; private set; } = new();

    // Navigation properties
    public virtual ICollection<ChatParticipant> Participants { get; private set; } = new List<ChatParticipant>();
    public virtual ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();

    protected Chat()
    {
    }

    public Chat(string title, ChatType type, Guid initiatorId, string? description = null, int priority = 1)
    {
        Title = title;
        Type = type;
        InitiatorId = initiatorId;
        Description = description;
        Status = ChatStatus.Pending;
        Priority = priority;
        LastActivityAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ChatCreatedEvent(Id, GetType().Name, type, initiatorId));
    }

    public void AssignOperator(Guid operatorId)
    {
        if (Status == ChatStatus.Closed)
            throw new InvalidOperationException("Cannot assign operator to closed chat");

        var previousOperatorId = AssignedOperatorId;
        AssignedOperatorId = operatorId;
        Status = ChatStatus.Active;
        LastActivityAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ChatAssignedEvent(Id, GetType().Name, operatorId, previousOperatorId));
    }

    public void TransferToOperator(Guid newOperatorId, string reason)
    {
        if (Status != ChatStatus.Active)
            throw new InvalidOperationException("Can only transfer active chats");

        var previousOperatorId = AssignedOperatorId;
        AssignedOperatorId = newOperatorId;
        Status = ChatStatus.Transferred;
        LastActivityAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ChatTransferredEvent(Id, GetType().Name, newOperatorId, previousOperatorId, reason));

        // Immediately set back to active with new operator
        Status = ChatStatus.Active;
    }

    public void CloseChat(string? reason = null)
    {
        if (Status == ChatStatus.Closed)
            return;

        Status = ChatStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        CloseReason = reason;

        AddDomainEvent(new ChatClosedEvent(Id, GetType().Name, AssignedOperatorId, reason));
    }

    public void MarkAsAbandoned()
    {
        if (Status == ChatStatus.Closed)
            return;

        Status = ChatStatus.Abandoned;
        ClosedAt = DateTimeOffset.UtcNow;
        CloseReason = "Customer abandoned chat";

        AddDomainEvent(new ChatAbandonedEvent(Id, GetType().Name, AssignedOperatorId));
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTimeOffset.UtcNow;
    }

    public void UpdateMetadata(string key, object value)
    {
        Metadata[key] = value;
    }

    public bool IsInactive(TimeSpan inactivityThreshold)
    {
        return LastActivityAt.HasValue &&
               DateTimeOffset.UtcNow - LastActivityAt.Value > inactivityThreshold;
    }

    public bool CanBeReassigned()
    {
        return Status == ChatStatus.Active && IsInactive(TimeSpan.FromMinutes(5));
    }
}