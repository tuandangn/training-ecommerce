using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Metadata;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;
using NamEcommerce.Domain.Values;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class GoodsReceiptMapping : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable(nameof(GoodsReceipt), DbScheme);
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Code).HasMaxLength(50).IsRequired().HasDefaultValue("");
        builder.HasIndex(g => g.Code).IsUnique().HasFilter("[Code] <> ''");

        builder.Property(g => g.ReceivedOnUtc).IsRequired();

        builder.Property(g => g.SourceType)
            .IsRequired()
            .HasDefaultValue(GoodsReceiptSourceType.FromVendor)
            .HasConversion<int>();

        builder.Property(g => g.BulkReceiveBatchId);
        builder.HasIndex(g => g.BulkReceiveBatchId);

        builder.ComplexProperty(g => g.TruckDriverName, driverNameBuilder =>
        {
            driverNameBuilder.Property(g => g.Value).HasColumnName(nameof(GoodsReceipt.TruckDriverName)).HasMaxLength(500).IsRequired(false);
            driverNameBuilder.Property(g => g.NormalizedValue).HasColumnName($"{nameof(GoodsReceipt.TruckDriverName)}Normalized").HasMaxLength(500).IsRequired(false);
        });
        builder.Property(g => g.TruckNumberSerial).HasMaxLength(100);
        builder.Property(g => g.Note).HasMaxLength(2000);

        builder.Property(g => g.VendorId);
        builder.ComplexProperty(g => g.VendorInfo, vendorInfoBuilder =>
        {
            vendorInfoBuilder.Property(info => info.Name)
                .HasColumnName($"Vendor{nameof(VendorInfo.Name)}")
                .HasMaxLength(500)
                .HasConversion(
                    info => info.Value,
                    value => new NormalizableString(value)
                );
            vendorInfoBuilder.Property(info => info.Address)
                .HasColumnName($"Vendor{nameof(VendorInfo.Address)}")
                .HasMaxLength(1000)
                .HasConversion(
                    info => info.Value,
                    value => new NormalizableString(value)
                );
            vendorInfoBuilder.Property(g => g.Phone).HasColumnName($"Vendor{nameof(VendorInfo.Phone)}").HasMaxLength(50);
        });

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

        builder.HasMany(g => g.Items)
            .WithOne()
            .HasForeignKey(i => i.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(g => g.Items).AutoInclude();
        builder.Metadata.FindNavigation(nameof(GoodsReceipt.Items))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
