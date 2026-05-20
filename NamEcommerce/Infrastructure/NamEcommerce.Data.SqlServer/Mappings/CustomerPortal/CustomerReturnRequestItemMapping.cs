using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerReturnRequestItemMapping : IEntityTypeConfiguration<CustomerReturnRequestItem>
{
    public void Configure(EntityTypeBuilder<CustomerReturnRequestItem> builder)
    {
        builder.ToTable(nameof(CustomerReturnRequestItem), DbScheme);
        builder.HasKey(item => item.Id);

        builder.Property(item => item.CustomerReturnRequestId).IsRequired();
        builder.Property(item => item.DeliveryNoteItemId).IsRequired();
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductName).HasMaxLength(500).IsRequired();
        builder.Property(item => item.RequestedQuantity).HasPrecision(18, 4).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired(false);

        builder.HasIndex(item => item.CustomerReturnRequestId);
    }
}
