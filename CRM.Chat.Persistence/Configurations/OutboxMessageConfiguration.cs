using CRM.Chat.Domain.Entities.OutboxMessages;
using CRM.Chat.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Chat.Persistence.Configurations;

public class OutboxMessageConfiguration : BaseEntityTypeConfiguration<OutboxMessage>
{
    public override void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable(nameof(OutboxMessage));

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Content)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.ProcessedAt);

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.Property(o => o.IsProcessed)
            .HasDefaultValue(false);

        builder.Property(o => o.AggregateId)
            .IsRequired();

        builder.Property(o => o.AggregateType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.RetryCount)
            .HasDefaultValue(0);

        builder.Property(o => o.InstanceId)
            .HasMaxLength(100);

        builder.Property(o => o.ClaimedAt);

        // Indexes
        builder.HasIndex(o => o.IsProcessed)
            .HasDatabaseName("IX_OutboxMessage_IsProcessed");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_OutboxMessage_CreatedAt");

        builder.HasIndex(o => new { o.IsProcessed, o.CreatedAt })
            .HasDatabaseName("IX_OutboxMessage_IsProcessed_CreatedAt");

        builder.HasIndex(o => o.AggregateId)
            .HasDatabaseName("IX_OutboxMessage_AggregateId");

        builder.HasIndex(o => o.Type)
            .HasDatabaseName("IX_OutboxMessage_Type");

        builder.HasIndex(o => o.InstanceId)
            .HasDatabaseName("IX_OutboxMessage_InstanceId");
    }
}