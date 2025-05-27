using System.Reflection;
using CRM.Chat.Application.Common.Abstractions.Users;
using CRM.Chat.Domain.Common.Entities;
using CRM.Chat.Domain.Entities.Messages;
using CRM.Chat.Domain.Entities.Operators;
using CRM.Chat.Domain.Entities.OutboxMessages;
using CRM.Chat.Domain.Entities.Participants;
using Microsoft.EntityFrameworkCore;

namespace CRM.Chat.Persistence.Databases;

public class ApplicationDbContext : DbContext
{
    private readonly IUserContext _userContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IUserContext userContext) : base(options)
    {
        _userContext = userContext;
    }

    public DbSet<Domain.Entities.Chats.Chat> Chats => Set<Domain.Entities.Chats.Chat>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<ChatOperator> ChatOperators => Set<ChatOperator>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private void ApplyAuditInformation()
    {
        var userId = _userContext.Id.ToString();
        var userIp = _userContext.IpAddress;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreationTracking(userId, userIp);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetModificationTracking(userId, userIp);

                    if (entry.Properties.Any(p => p.Metadata.Name == nameof(AuditableEntity.IsDeleted)) &&
                        entry.Property(nameof(AuditableEntity.IsDeleted)).CurrentValue is true &&
                        entry.Property(nameof(AuditableEntity.IsDeleted)).OriginalValue is false)
                    {
                        entry.Entity.SetDeletionTracking(userId, userIp);
                    }

                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.SetDeletionTracking(userId, userIp);
                    break;
            }
        }
    }
}