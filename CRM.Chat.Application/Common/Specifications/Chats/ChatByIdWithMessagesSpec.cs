namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class ChatByIdWithMessagesSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public ChatByIdWithMessagesSpec(Guid chatId) : base(c => c.Id == chatId)
    {
        AddInclude(c => c.Messages);
        AddInclude(c => c.Participants);
    }
}