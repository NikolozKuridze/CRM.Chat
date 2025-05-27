
namespace CRM.Chat.Application.Common.Specifications.Chats;

public sealed class ChatsByTypeSpec : BaseSpecification<Domain.Entities.Chats.Chat>
{
    public ChatsByTypeSpec(ChatType chatType) : base(c => c.Type == chatType)
    {
        AddInclude(c => c.Messages);
        AddInclude(c => c.Participants);
    }
}