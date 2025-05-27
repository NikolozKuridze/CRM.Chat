namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class ChatsByOperatorIdSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public ChatsByOperatorIdSpec(Guid operatorId) : base(c => c.AssignedOperatorId == operatorId)
    {
        AddInclude(c => c.Messages);
        AddInclude(c => c.Participants);
    }
}