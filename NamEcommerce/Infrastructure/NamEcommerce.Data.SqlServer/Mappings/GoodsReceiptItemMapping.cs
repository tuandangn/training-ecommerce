using NamEcommerce.Domain.Entities.GoodsReceipts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class GoodsReceiptItemMapping : IEntityTypeConfiguration<GoodsReceiptItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
    {
        builder.ToTable(nameof(GoodsReceiptItem), DbScheme);
        builder.HasKey(i => i.Id);

        // GoodsReceiptId là readonly field — dùng field accessor để EF Core có thể ghi khi materialize
        builder.Property(i => i.GoodsReceiptId)
            .HasField("goodsReceiptId")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).HasMaxLength(500).IsRequired();

        builder.Property(i => i.WarehouseId);
        builder.Property(i => i.WarehouseName).HasMaxLength(500);

        builder.Property(i => i.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(i => i.UnitCost).HasColumnType("decimal(18,2)");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
