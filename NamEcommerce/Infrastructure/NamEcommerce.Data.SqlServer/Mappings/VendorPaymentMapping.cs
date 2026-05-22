using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorPaymentMapping : IEntityTypeConfiguration<VendorPayment>
{
    public void Configure(EntityTypeBuilder<VendorPayment> builder)
    {
        builder.ToTable(nameof(VendorPayment), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.VendorId).IsRequired();
        builder.HasIndex(x => x.VendorId);

        builder.Property(x => x.VendorName).IsRequired().HasMaxLength(255);

        builder.Property(x => x.VendorDebtId).IsRequired(false);
        builder.HasIndex(x => x.VendorDebtId);

        builder.Property(x => x.PurchaseOrderId).IsRequired(false);
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.Property(x => x.PurchaseOrderCode).IsRequired(false).HasMaxLength(100);

        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaymentMethod).IsRequired();
        builder.Property(x => x.PaymentType).IsRequired();
        builder.Property(x => x.Note).IsRequired(false).HasMaxLength(1000);

        builder.Property(x => x.PaidOnUtc).IsRequired();
        builder.Property(x => x.RecordedByUserId).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);

        builder.Property(x => x.IsApplied).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.AppliedOnUtc).IsRequired(false);
    }
}
