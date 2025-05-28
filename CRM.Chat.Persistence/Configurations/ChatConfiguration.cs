using System.Text.Json;
using CRM.Chat.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Chat.Persistence.Configurations;

public class ChatConfiguration : AuditableEntityTypeConfiguration<Domain.Entities.Chats.Chat>
{
    public override void Configure(EntityTypeBuilder<Domain.Entities.Chats.Chat> builder)
    {
        base.Configure(builder);

        builder.ToTable(nameof(Chat));

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.InitiatorId)
            .IsRequired();

        builder.Property(c => c.AssignedOperatorId);

        builder.Property(c => c.LastActivityAt);

        builder.Property(c => c.ClosedAt);

        builder.Property(c => c.CloseReason)
            .HasMaxLength(500);

        builder.Property(c => c.Priority)
            .HasDefaultValue(1);

        // Configure Metadata as JSON column with Value Comparer
        builder.Property(c => c.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions()) ??
                     new Dictionary<string, object>())
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => new Dictionary<string, object>(c)));

        // Relationships
        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Chat)
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.Chat)
            .HasForeignKey(p => p.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.InitiatorId)
            .HasDatabaseName("IX_Chat_InitiatorId");

        builder.HasIndex(c => c.AssignedOperatorId)
            .HasDatabaseName("IX_Chat_AssignedOperatorId");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Chat_Status");

        builder.HasIndex(c => c.Type)
            .HasDatabaseName("IX_Chat_Type");

        builder.HasIndex(c => c.LastActivityAt)
            .HasDatabaseName("IX_Chat_LastActivityAt");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Chat_CreatedAt");

        builder.HasIndex(c => new { c.Status, c.AssignedOperatorId })
            .HasDatabaseName("IX_Chat_Status_AssignedOperatorId");
    }
}