using NamEcommerce.Domain.Entities.Notifications;

namespace NamEcommerce.Data.SqlServer.Mappings.Notifications;

public sealed class SystemNotificationMap : IEntityTypeConfiguration<SystemNotification>
{
    public void Configure(EntityTypeBuilder<SystemNotification> builder)
    {
        builder.ToTable(nameof(SystemNotification), DbScheme);
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.Severity).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(250).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired(false);
        builder.Property(notification => notification.RequiredPermission).HasMaxLength(200).IsRequired();
        builder.Property(notification => notification.RelatedEntityType).HasMaxLength(100).IsRequired(false);
        builder.Property(notification => notification.RelatedEntityId).IsRequired(false);
        builder.Property(notification => notification.ActionUrl).HasMaxLength(500).IsRequired(false);
        builder.Property(notification => notification.CreatedByUserId).IsRequired(false);
        builder.Property(notification => notification.CreatedOnUtc).IsRequired();

        builder.HasIndex(notification => new { notification.RequiredPermission, notification.CreatedOnUtc });
        builder.HasIndex(notification => new { notification.Type, notification.CreatedOnUtc });
        builder.HasIndex(notification => notification.RelatedEntityId);
    }
}
