namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class InactiveChatsSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public InactiveChatsSpec(TimeSpan inactivityThreshold)
    {
        var cutoffTime = DateTimeOffset.UtcNow - inactivityThreshold;

        Criteria = c => c.Status == ChatStatus.Active &&
                        c.LastActivityAt.HasValue &&
                        c.LastActivityAt.Value < cutoffTime;
    }
}