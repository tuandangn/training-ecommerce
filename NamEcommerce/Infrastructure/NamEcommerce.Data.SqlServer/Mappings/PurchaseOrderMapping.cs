using NamEcommerce.Domain.Entities.PurchaseOrders;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class PurchaseOrderMapping : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable(nameof(PurchaseOrder), DbScheme);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.VendorId).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.ExpectedDeliveryDateUtc);
        builder.Property(p => p.Note).HasMaxLength(1000);
        builder.Property(p => p.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ShippingAmount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CreatedOnUtc).IsRequired();
        builder.Property(p => p.UpdatedOnUtc);

        builder.Property(p => p.WarehouseId);
        builder.Property(p => p.CreatedByUserId);

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Items).AutoInclude();
    }
}
