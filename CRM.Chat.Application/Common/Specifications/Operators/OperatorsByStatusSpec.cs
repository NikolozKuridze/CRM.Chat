using CRM.Chat.Domain.Entities.Operators;
using CRM.Chat.Domain.Entities.Operators.Enums;

namespace CRM.Chat.Application.Common.Specifications.Operators;

public sealed class OperatorsByStatusSpec : BaseSpecification<ChatOperator>
{
    public OperatorsByStatusSpec(OperatorStatus status) : base(o => o.Status == status)
    {
        ApplyOrderBy(o => o.CurrentChatCount);
    }
}