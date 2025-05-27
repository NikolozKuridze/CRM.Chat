namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class InactiveChatsSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public InactiveChatsSpec(TimeSpan inactivityThreshold)
        : base(c => c.Status == ChatStatus.Active && 
                    c.LastActivityAt.HasValue && 
                    DateTimeOffset.UtcNow.Subtract(c.LastActivityAt.Value) > inactivityThreshold)
    {
    }
}