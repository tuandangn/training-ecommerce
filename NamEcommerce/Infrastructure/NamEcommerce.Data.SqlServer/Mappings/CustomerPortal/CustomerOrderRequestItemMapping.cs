using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerOrderRequestItemMapping : IEntityTypeConfiguration<CustomerOrderRequestItem>
{
    public void Configure(EntityTypeBuilder<CustomerOrderRequestItem> builder)
    {
        builder.ToTable(nameof(CustomerOrderRequestItem), DbScheme);
        builder.HasKey(item => item.Id);

        builder.Property(item => item.CustomerOrderRequestId).IsRequired();
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductName).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(item => item.UnitPriceSnapshot).HasPrecision(18, 2).IsRequired();

        builder.Ignore(item => item.SubTotal);
        builder.HasIndex(item => item.CustomerOrderRequestId);
    }
}
