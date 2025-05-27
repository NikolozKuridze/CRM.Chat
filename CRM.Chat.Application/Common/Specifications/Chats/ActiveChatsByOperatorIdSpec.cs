namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class ActiveChatsByOperatorIdSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public ActiveChatsByOperatorIdSpec(Guid operatorId) 
        : base(c => c.AssignedOperatorId == operatorId && c.Status == ChatStatus.Active)
    {
        AddInclude(c => c.Messages);
    }
}