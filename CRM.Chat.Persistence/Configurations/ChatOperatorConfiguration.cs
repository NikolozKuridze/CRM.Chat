using System.Text.Json;
using CRM.Chat.Domain.Entities.Operators;
using CRM.Chat.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Chat.Persistence.Configurations;

public class ChatOperatorConfiguration : AuditableEntityTypeConfiguration<ChatOperator>
{
    public override void Configure(EntityTypeBuilder<ChatOperator> builder)
    {
        base.Configure(builder);

        builder.ToTable(nameof(ChatOperator));

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.MaxConcurrentChats)
            .HasDefaultValue(5);

        builder.Property(o => o.CurrentChatCount)
            .HasDefaultValue(0);

        builder.Property(o => o.LastActiveAt);

        builder.Property(o => o.IsOnline)
            .HasDefaultValue(false);

        // Configure Skills as JSON array with Value Comparer
        builder.Property(o => o.Skills)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>())
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => new List<string>(c)));

        // Configure Metadata as JSON column with Value Comparer
        builder.Property(o => o.Metadata)
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
        builder.HasIndex(o => o.UserId)
            .IsUnique()
            .HasDatabaseName("IX_ChatOperator_UserId");

        builder.HasIndex(o => o.Email)
            .IsUnique()
            .HasDatabaseName("IX_ChatOperator_Email");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_ChatOperator_Status");

        builder.HasIndex(o => o.IsOnline)
            .HasDatabaseName("IX_ChatOperator_IsOnline");

        builder.HasIndex(o => new { o.Status, o.IsOnline, o.CurrentChatCount })
            .HasDatabaseName("IX_ChatOperator_Status_IsOnline_CurrentChatCount");

        builder.HasIndex(o => o.LastActiveAt)
            .HasDatabaseName("IX_ChatOperator_LastActiveAt");
    }
}