using NamEcommerce.Domain.Entities.GoodsReceipts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class GoodsReceiptMapping : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable(nameof(GoodsReceipt), DbScheme);
        builder.HasKey(g => g.Id);

        builder.Property(g => g.CreatedOnUtc).IsRequired();

        builder.Property(g => g.TruckDriverName).HasMaxLength(500);
        builder.Property("TruckDriverNameNormalized").HasMaxLength(500);
        builder.Property(g => g.TruckNumberSerial).HasMaxLength(100);
        builder.Property(g => g.Note).HasMaxLength(2000);

        // Vendor snapshot — nullable
        builder.Property(g => g.VendorId);
        builder.Property(g => g.VendorName).HasMaxLength(500);
        builder.Property(g => g.VendorPhone).HasMaxLength(50);
        builder.Property(g => g.VendorAddress).HasMaxLength(1000);

        builder.Property(g => g.CreatedByUserId);
        builder.Property(g => g.CreatedByUsername).HasMaxLength(500);

        // PictureIds — danh sách Guid chứng từ, lưu dưới dạng JSON
        builder.Property<IList<Guid>>("_pictureIds")
            .HasColumnName("PictureIds")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<Guid>()
                    : System.Text.Json.JsonSerializer.Deserialize<IList<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                      ?? new List<Guid>());

        // Items collection — dùng backing field _items
        builder.Metadata.FindNavigation(nameof(GoodsReceipt.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(g => g.Items)
            .WithOne()
            .HasForeignKey(i => i.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Items).AutoInclude();
    }
}
