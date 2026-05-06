using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class GoodsReceiptMapping : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable(nameof(GoodsReceipt), DbScheme);
        builder.HasKey(g => g.Id);

        builder.Property(g => g.ReceivedOnUtc).IsRequired();

        // Phase A1: SourceType phân biệt nguồn (NCC vs trả từ KH vs điều chỉnh).
        // Default = FromVendor để dữ liệu hiện có (chưa có cột) sau migration tự map đúng.
        builder.Property(g => g.SourceType)
            .IsRequired()
            .HasDefaultValue(GoodsReceiptSourceType.FromVendor)
            .HasConversion<int>();

        builder.Property(g => g.TruckDriverName).HasMaxLength(500);
        builder.Property(g => g.TruckDriverNameNormalized).HasMaxLength(500);
        builder.Property(g => g.TruckNumberSerial).HasMaxLength(100);
        builder.Property(g => g.Note).HasMaxLength(2000);

        builder.Property(g => g.VendorId);
        builder.Property(g => g.VendorName).HasMaxLength(500);
        builder.Property(g => g.VendorPhone).HasMaxLength(50);
        builder.Property(g => g.VendorAddress).HasMaxLength(1000);

        builder.Property(g => g.CreatedByUserId);
        builder.Property(g => g.CreatedByUsername).HasMaxLength(500);

        builder.Property<IList<Guid>>("_pictureIds")
            .HasColumnName("PictureIds")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<Guid>()
                    : System.Text.Json.JsonSerializer.Deserialize<IList<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                      ?? new List<Guid>());

        builder.Metadata.FindNavigation(nameof(GoodsReceipt.Items))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(g => g.Items)
            .WithOne()
            .HasForeignKey(i => i.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Items).AutoInclude();
    }
}
