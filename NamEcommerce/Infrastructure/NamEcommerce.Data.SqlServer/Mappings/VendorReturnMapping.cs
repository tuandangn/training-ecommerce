using NamEcommerce.Domain.Entities.Returns;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorReturnMapping : IEntityTypeConfiguration<VendorReturn>
{
    public void Configure(EntityTypeBuilder<VendorReturn> builder)
    {
        builder.ToTable(nameof(VendorReturn), DbScheme);
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.VendorId).IsRequired();
        builder.Property(r => r.VendorName).IsRequired().HasMaxLength(500);

        builder.Property(r => r.PurchaseOrderId).IsRequired(false);
        builder.Property(r => r.GoodsReceiptId).IsRequired(false);

        builder.Property(r => r.WarehouseId).IsRequired();
        builder.Property(r => r.WarehouseName).IsRequired().HasMaxLength(500);

        builder.Property(r => r.Note).HasMaxLength(2000);
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();
        builder.Property(r => r.ReturnDate).IsRequired();
        builder.Property(r => r.ConfirmedOnUtc).IsRequired(false);
        builder.Property(r => r.ReversedOnUtc).IsRequired(false);
        builder.Property(r => r.ReversedReason).HasMaxLength(1000);
        builder.Property(r => r.InspectedByUserId).IsRequired(false);
        builder.Property(r => r.InspectedOnUtc).IsRequired(false);
        builder.Property(r => r.AdditionalCost).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        builder.Property(r => r.GeneratedDeliveryNoteId).IsRequired(false);

        builder.Property(r => r.CreatedByUserId).IsRequired(false);
        builder.Property(r => r.CreatedOnUtc).IsRequired();
        builder.Property(r => r.UpdatedOnUtc).IsRequired(false);

        // Index hỗ trợ query theo nguồn + filter theo Status
        builder.HasIndex(r => new { r.PurchaseOrderId, r.Status });
        builder.HasIndex(r => new { r.GoodsReceiptId, r.Status });
        builder.HasIndex(r => new { r.VendorId, r.Status });

        // Navigation: _items (private backing field)
        builder.Metadata.FindNavigation(nameof(VendorReturn.Items))
            ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.VendorReturnId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Items).AutoInclude();
    }
}
