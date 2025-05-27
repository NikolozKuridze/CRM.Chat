using CRM.Chat.Domain.Entities.Operators;

namespace CRM.Chat.Application.Common.Specifications.Operators;

public sealed class OperatorByUserIdSpec : BaseSpecification<ChatOperator>
{
    public OperatorByUserIdSpec(Guid userId) : base(o => o.UserId == userId)
    {
    }
}