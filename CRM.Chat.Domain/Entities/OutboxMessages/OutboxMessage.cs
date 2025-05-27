using CRM.Chat.Domain.Common.Entities;

namespace CRM.Chat.Domain.Entities.OutboxMessages;

public sealed class OutboxMessage : Entity
{
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsProcessed { get; private set; }
    public Guid AggregateId { get; private set; }
    public string AggregateType { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public string? InstanceId { get; private set; }
    public DateTimeOffset? ClaimedAt { get; private set; }

    public OutboxMessage(string type, string content, Guid aggregateId, string aggregateType)
    {
        Type = type;
        Content = content;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        CreatedAt = DateTimeOffset.UtcNow;
        IsProcessed = false;
        RetryCount = 0;
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTimeOffset.UtcNow;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }

    public bool CanBeRetried(int maxRetries = 3)
    {
        return RetryCount < maxRetries;
    }

    public void Claim(string instanceId)
    {
        InstanceId = instanceId;
        ClaimedAt = DateTimeOffset.UtcNow;
    }

    public bool IsClaimedBy(string instanceId)
    {
        return InstanceId == instanceId;
    }

    public bool IsExpiredClaim(TimeSpan claimTimeout)
    {
        return ClaimedAt.HasValue &&
               DateTimeOffset.UtcNow - ClaimedAt.Value > claimTimeout;
    }
}