using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerPortalNotificationMapping : IEntityTypeConfiguration<CustomerPortalNotification>
{
    public void Configure(EntityTypeBuilder<CustomerPortalNotification> builder)
    {
        builder.ToTable(nameof(CustomerPortalNotification), DbScheme);
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.CustomerId).IsRequired();
        builder.Property(notification => notification.Type).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.Status).HasConversion<int>().IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(250).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired(false);
        builder.Property(notification => notification.RelatedEntityId).IsRequired(false);
        builder.Property(notification => notification.RelatedEntityType).HasMaxLength(100).IsRequired(false);
        builder.Property(notification => notification.CreatedOnUtc).IsRequired();
        builder.Property(notification => notification.ReadOnUtc).IsRequired(false);
        builder.Property(notification => notification.ReadByUserId).IsRequired(false);

        builder.HasIndex(notification => new { notification.Status, notification.CreatedOnUtc });
        builder.HasIndex(notification => new { notification.CustomerId, notification.CreatedOnUtc });
        builder.HasIndex(notification => notification.RelatedEntityId);
    }
}
