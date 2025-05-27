using CRM.Chat.Domain.Entities.Participants;
using CRM.Chat.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Chat.Persistence.Configurations;

public class ChatParticipantConfiguration : AuditableEntityTypeConfiguration<ChatParticipant>
{
    public override void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        base.Configure(builder);

        builder.ToTable(nameof(ChatParticipant));

        builder.Property(p => p.ChatId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.JoinedAt)
            .IsRequired();

        builder.Property(p => p.LeftAt);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.LastSeenAt);

        builder.Property(p => p.UnreadCount)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(p => p.ChatId)
            .HasDatabaseName("IX_ChatParticipant_ChatId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_ChatParticipant_UserId");

        builder.HasIndex(p => new { p.ChatId, p.UserId })
            .IsUnique()
            .HasDatabaseName("IX_ChatParticipant_ChatId_UserId");

        builder.HasIndex(p => new { p.UserId, p.IsActive })
            .HasDatabaseName("IX_ChatParticipant_UserId_IsActive");
    }
}