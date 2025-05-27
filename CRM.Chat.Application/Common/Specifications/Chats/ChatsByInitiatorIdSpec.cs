namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class ChatsByInitiatorIdSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public ChatsByInitiatorIdSpec(Guid initiatorId) : base(c => c.InitiatorId == initiatorId)
    {
        AddInclude(c => c.Messages);
        ApplyOrderByDescending(c => c.CreatedAt);
    }
}