using System.ComponentModel.DataAnnotations.Schema;
using CRM.Chat.Domain.Common.Events;

namespace CRM.Chat.Domain.Common.Entities;

public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped] public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}