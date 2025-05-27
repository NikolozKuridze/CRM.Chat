using CRM.Chat.Application.Common.Persistence;
using CRM.Chat.Domain.Common.Entities;
using CRM.Chat.Domain.Common.Events;
using CRM.Chat.Domain.Entities.OutboxMessages;
using CRM.Chat.Persistence.Databases;

namespace CRM.Chat.Persistence.Utils;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var domainEvents = GetDomainEventsFromTrackedEntities();
            ClearDomainEvents();

            var result = await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // In a real-world scenario, you would publish domain events here
            // For now, we'll just log them or store them in outbox
            await ProcessDomainEventsAsync(domainEvents, cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private IReadOnlyList<IDomainEvent> GetDomainEventsFromTrackedEntities()
    {
        var domainEvents = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        return domainEvents;
    }

    private void ClearDomainEvents()
    {
        _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .ToList()
            .ForEach(e => e.Entity.ClearDomainEvents());
    }

    private async Task ProcessDomainEventsAsync(IReadOnlyList<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            // Create outbox message for each domain event
            var outboxMessage = new OutboxMessage(
                domainEvent.EventType,
                System.Text.Json.JsonSerializer.Serialize(domainEvent),
                domainEvent.AggregateId,
                domainEvent.AggregateType);

            await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        }

        // Save outbox messages in the same transaction
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}