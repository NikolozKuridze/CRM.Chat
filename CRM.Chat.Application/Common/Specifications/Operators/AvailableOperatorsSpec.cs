using CRM.Chat.Domain.Entities.Operators;
using CRM.Chat.Domain.Entities.Operators.Enums;

namespace CRM.Chat.Application.Common.Specifications.Operators;

public sealed class AvailableOperatorsSpec : BaseSpecification<ChatOperator>
{
    public AvailableOperatorsSpec() 
        : base(o => o.IsOnline && o.Status == OperatorStatus.Available && o.CurrentChatCount < o.MaxConcurrentChats)
    {
        ApplyOrderBy(o => o.CurrentChatCount);
    }
}
