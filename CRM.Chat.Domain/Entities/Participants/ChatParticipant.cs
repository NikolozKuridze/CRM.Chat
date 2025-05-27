using CRM.Chat.Domain.Entities.Participants.Enums;

namespace CRM.Chat.Domain.Entities.Participants;

public class ChatParticipant : AuditableEntity
{
    public Guid ChatId { get; private set; }
    public Guid UserId { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? LeftAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastSeenAt { get; private set; }
    public int UnreadCount { get; private set; } = 0;

    // Navigation properties
    public virtual Chats.Chat Chat { get; private set; } = null!;

    protected ChatParticipant()
    {
    }

    public ChatParticipant(Guid chatId, Guid userId, ParticipantRole role)
    {
        ChatId = chatId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTimeOffset.UtcNow;
        LastSeenAt = DateTimeOffset.UtcNow;
    }

    public void LeaveChat()
    {
        IsActive = false;
        LeftAt = DateTimeOffset.UtcNow;
    }

    public void RejoinChat()
    {
        IsActive = true;
        LeftAt = null;
        LastSeenAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTimeOffset.UtcNow;
    }

    public void IncrementUnreadCount()
    {
        UnreadCount++;
    }

    public void ResetUnreadCount()
    {
        UnreadCount = 0;
    }

    public void ChangeRole(ParticipantRole newRole)
    {
        Role = newRole;
    }
}