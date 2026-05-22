using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerSecurityEventMapping : IEntityTypeConfiguration<CustomerSecurityEvent>
{
    public void Configure(EntityTypeBuilder<CustomerSecurityEvent> builder)
    {
        builder.ToTable(nameof(CustomerSecurityEvent), DbScheme);
        builder.HasKey(securityEvent => securityEvent.Id);

        builder.Property(securityEvent => securityEvent.CustomerId).IsRequired(false);
        builder.Property(securityEvent => securityEvent.DeliveryNoteId).IsRequired(false);
        builder.Property(securityEvent => securityEvent.EventType).HasMaxLength(100).IsRequired();
        builder.Property(securityEvent => securityEvent.Outcome).HasConversion<int>().IsRequired();
        builder.Property(securityEvent => securityEvent.IpAddress).HasMaxLength(100).IsRequired(false);
        builder.Property(securityEvent => securityEvent.UserAgent).HasMaxLength(500).IsRequired(false);
        builder.Property(securityEvent => securityEvent.MetadataJson).HasMaxLength(4000).IsRequired(false);
        builder.Property(securityEvent => securityEvent.CreatedOnUtc).IsRequired();

        builder.HasIndex(securityEvent => new { securityEvent.CustomerId, securityEvent.EventType, securityEvent.CreatedOnUtc });
        builder.HasIndex(securityEvent => new { securityEvent.IpAddress, securityEvent.EventType, securityEvent.CreatedOnUtc });
    }
}
