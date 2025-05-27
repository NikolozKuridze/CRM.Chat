using CRM.Chat.Domain.Entities.Operators.DomainEvents;
using CRM.Chat.Domain.Entities.Operators.Enums;

namespace CRM.Chat.Domain.Entities.Operators;

public class ChatOperator : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public OperatorStatus Status { get; private set; } = OperatorStatus.Offline;
    public int MaxConcurrentChats { get; private set; } = 5;
    public int CurrentChatCount { get; private set; } = 0;
    public DateTimeOffset? LastActiveAt { get; private set; }
    public bool IsOnline { get; private set; } = false;
    public List<string> Skills { get; private set; } = new();
    public Dictionary<string, object> Metadata { get; private set; } = new();

    protected ChatOperator()
    {
    }

    public ChatOperator(Guid userId, string displayName, string email, int maxConcurrentChats = 5)
    {
        UserId = userId;
        DisplayName = displayName;
        Email = email;
        MaxConcurrentChats = maxConcurrentChats;
        Status = OperatorStatus.Offline;

        AddDomainEvent(new OperatorCreatedEvent(Id, GetType().Name, userId, displayName));
    }

    public void SetOnline()
    {
        IsOnline = true;
        Status = OperatorStatus.Available;
        LastActiveAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OperatorStatusChangedEvent(Id, GetType().Name, UserId, OperatorStatus.Available));
    }

    public void SetOffline()
    {
        IsOnline = false;
        Status = OperatorStatus.Offline;

        AddDomainEvent(new OperatorStatusChangedEvent(Id, GetType().Name, UserId, OperatorStatus.Offline));
    }

    public void SetStatus(OperatorStatus status)
    {
        if (!IsOnline && status != OperatorStatus.Offline)
            throw new InvalidOperationException("Cannot set status when offline");

        Status = status;
        LastActiveAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OperatorStatusChangedEvent(Id, GetType().Name, UserId, status));
    }

    public void AssignChat()
    {
        if (!CanAcceptNewChat())
            throw new InvalidOperationException("Operator cannot accept new chats");

        CurrentChatCount++;
        UpdateStatus();
        LastActiveAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OperatorChatAssignedEvent(Id, GetType().Name, UserId, CurrentChatCount));
    }

    public void UnassignChat()
    {
        if (CurrentChatCount > 0)
        {
            CurrentChatCount--;
            UpdateStatus();
            LastActiveAt = DateTimeOffset.UtcNow;

            AddDomainEvent(new OperatorChatUnassignedEvent(Id, GetType().Name, UserId, CurrentChatCount));
        }
    }

    public bool CanAcceptNewChat()
    {
        return IsOnline &&
               Status == OperatorStatus.Available &&
               CurrentChatCount < MaxConcurrentChats;
    }

    public void UpdateMaxConcurrentChats(int maxChats)
    {
        if (maxChats < 1)
            throw new ArgumentException("Max concurrent chats must be at least 1");

        MaxConcurrentChats = maxChats;
        UpdateStatus();
    }

    public void AddSkill(string skill)
    {
        if (!Skills.Contains(skill))
        {
            Skills.Add(skill);
        }
    }

    public void RemoveSkill(string skill)
    {
        Skills.Remove(skill);
    }

    public double GetWorkloadPercentage()
    {
        return MaxConcurrentChats > 0 ? (double)CurrentChatCount / MaxConcurrentChats * 100 : 0;
    }

    public void UpdateActivity()
    {
        LastActiveAt = DateTimeOffset.UtcNow;
    }

    private void UpdateStatus()
    {
        if (!IsOnline) return;

        var newStatus = CurrentChatCount >= MaxConcurrentChats
            ? OperatorStatus.Busy
            : OperatorStatus.Available;

        if (newStatus != Status)
        {
            Status = newStatus;
            AddDomainEvent(new OperatorStatusChangedEvent(Id, GetType().Name, UserId, newStatus));
        }
    }
}