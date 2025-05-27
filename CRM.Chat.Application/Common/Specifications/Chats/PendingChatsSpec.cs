namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class PendingChatsSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public PendingChatsSpec() : base(c => c.Status == ChatStatus.Pending)
    {
        ApplyOrderBy(c => c.CreatedAt);
    }
}