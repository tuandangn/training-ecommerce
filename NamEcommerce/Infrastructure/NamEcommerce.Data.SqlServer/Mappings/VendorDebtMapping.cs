using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorDebtMapping : IEntityTypeConfiguration<VendorDebt>
{
    public void Configure(EntityTypeBuilder<VendorDebt> builder)
    {
        builder.ToTable(nameof(VendorDebt), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.VendorId).IsRequired();
        builder.HasIndex(x => x.VendorId);

        builder.Property(x => x.VendorName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.NormalizedVendorName).HasMaxLength(400);
        builder.Property(x => x.VendorPhone).IsRequired(false).HasMaxLength(50);
        builder.Property(x => x.NormalizedVendorPhone).HasMaxLength(100);
        builder.Property(x => x.VendorAddress).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.NormalizedVendorAddress).HasMaxLength(1000);

        builder.Property(x => x.PurchaseOrderId).IsRequired(false);
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.Property(x => x.PurchaseOrderCode).IsRequired(false).HasMaxLength(100);

        builder.Property(x => x.GoodsReceiptId).IsRequired(false);
        builder.HasIndex(x => x.GoodsReceiptId);

        builder.Property(x => x.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.RemainingAmount).IsRequired().HasColumnType("decimal(18,2)");

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.DueDateUtc).IsRequired(false);

        builder.Property(x => x.CreatedByUserId).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
    }
}
