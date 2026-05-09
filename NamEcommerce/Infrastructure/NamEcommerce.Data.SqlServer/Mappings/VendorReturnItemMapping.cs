using NamEcommerce.Domain.Entities.Returns;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorReturnItemMapping : IEntityTypeConfiguration<VendorReturnItem>
{
    public void Configure(EntityTypeBuilder<VendorReturnItem> builder)
    {
        builder.ToTable(nameof(VendorReturnItem), DbScheme);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.VendorReturnId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(500);

        builder.Property(i => i.GoodsReceiptItemId).IsRequired(false);

        builder.Property(i => i.RequestedQuantity).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(i => i.AcceptedQuantity).IsRequired().HasColumnType("decimal(18,2)");

        // Giá vốn gốc (tham chiếu) — nullable
        builder.Property(i => i.OriginalUnitCost).IsRequired(false).HasColumnType("decimal(18,4)");

        // Giá NCC hoàn trả thực tế — required
        builder.Property(i => i.ReturnUnitCost).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
    }
}
