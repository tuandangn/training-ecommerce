using NamEcommerce.Domain.Entities.Returns;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerReturnItemMapping : IEntityTypeConfiguration<CustomerReturnItem>
{
    public void Configure(EntityTypeBuilder<CustomerReturnItem> builder)
    {
        builder.ToTable(nameof(CustomerReturnItem), DbScheme);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.CustomerReturnId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(500);

        builder.Property(i => i.DeliveryNoteItemId).IsRequired(false);

        builder.Property(i => i.RequestedQuantity).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(i => i.AcceptedQuantity).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(i => i.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
    }
}
