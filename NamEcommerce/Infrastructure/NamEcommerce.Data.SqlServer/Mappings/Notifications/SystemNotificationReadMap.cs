using NamEcommerce.Domain.Entities.Notifications;

namespace NamEcommerce.Data.SqlServer.Mappings.Notifications;

public sealed class SystemNotificationReadMap : IEntityTypeConfiguration<SystemNotificationRead>
{
    public void Configure(EntityTypeBuilder<SystemNotificationRead> builder)
    {
        builder.ToTable(nameof(SystemNotificationRead), DbScheme);
        builder.HasKey(read => read.Id);

        builder.Property(read => read.NotificationId).IsRequired();
        builder.Property(read => read.UserId).IsRequired();
        builder.Property(read => read.ReadOnUtc).IsRequired();

        builder.HasIndex(read => new { read.NotificationId, read.UserId }).IsUnique();
        builder.HasIndex(read => new { read.UserId, read.ReadOnUtc });
    }
}
