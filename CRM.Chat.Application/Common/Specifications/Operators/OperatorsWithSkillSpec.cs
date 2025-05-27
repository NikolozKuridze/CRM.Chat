using CRM.Chat.Domain.Entities.Operators;
using CRM.Chat.Domain.Entities.Operators.Enums;

namespace CRM.Chat.Application.Common.Specifications.Operators;

public sealed class OperatorsWithSkillSpec : BaseSpecification<ChatOperator>
{
    public OperatorsWithSkillSpec(string skill) 
        : base(o => o.Skills.Contains(skill) && o.IsOnline && o.Status == OperatorStatus.Available)
    {
        ApplyOrderBy(o => o.CurrentChatCount);
    }
}