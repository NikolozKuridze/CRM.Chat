using CRM.Chat.Domain.Entities.Operators;

namespace CRM.Chat.Application.Common.Specifications.Operators;

public sealed class OnlineOperatorsSpec : BaseSpecification<ChatOperator>
{
    public OnlineOperatorsSpec() : base(o => o.IsOnline)
    {
        ApplyOrderBy(o => o.DisplayName);
    }
}