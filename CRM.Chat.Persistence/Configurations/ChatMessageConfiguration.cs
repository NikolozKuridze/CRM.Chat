using System.Text.Json;
using CRM.Chat.Domain.Entities.Messages;
using CRM.Chat.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Chat.Persistence.Configurations;

public class ChatMessageConfiguration : AuditableEntityTypeConfiguration<ChatMessage>
{
    public override void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable(nameof(ChatMessage));

        builder.Property(m => m.ChatId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired();

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.IsRead)
            .HasDefaultValue(false);

        builder.Property(m => m.ReadAt);

        builder.Property(m => m.ReadBy);

        builder.Property(m => m.IsEdited)
            .HasDefaultValue(false);

        builder.Property(m => m.EditedAt);

        builder.Property(m => m.OriginalContent)
            .HasMaxLength(4000);

        // Configure Metadata as JSON column with Value Comparer
        builder.Property(m => m.Metadata)
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

        // Indexes
        builder.HasIndex(m => m.ChatId)
            .HasDatabaseName("IX_ChatMessage_ChatId");

        builder.HasIndex(m => m.SenderId)
            .HasDatabaseName("IX_ChatMessage_SenderId");

        builder.HasIndex(m => m.CreatedAt)
            .HasDatabaseName("IX_ChatMessage_CreatedAt");

        builder.HasIndex(m => new { m.ChatId, m.CreatedAt })
            .HasDatabaseName("IX_ChatMessage_ChatId_CreatedAt");

        builder.HasIndex(m => new { m.ChatId, m.IsRead })
            .HasDatabaseName("IX_ChatMessage_ChatId_IsRead");
    }
}