using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorRefundMapping : IEntityTypeConfiguration<VendorRefund>
{
    public void Configure(EntityTypeBuilder<VendorRefund> builder)
    {
        builder.ToTable(nameof(VendorRefund), DbScheme);
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.VendorId).IsRequired();
        builder.Property(r => r.VendorName).IsRequired().HasMaxLength(255);

        builder.Property(r => r.VendorReturnId).IsRequired();
        builder.Property(r => r.VendorReturnCode).IsRequired().HasMaxLength(50);

        builder.Property(r => r.VendorDebtId).IsRequired(false);

        builder.Property(r => r.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();
        builder.Property(r => r.PaymentMethod).IsRequired(false).HasConversion<int>();
        builder.Property(r => r.BankAccountId).IsRequired(false);

        builder.Property(r => r.Note).HasMaxLength(2000);
        builder.Property(r => r.RefundedOnUtc).IsRequired(false);
        builder.Property(r => r.CompletedByUserId).IsRequired(false);
        builder.Property(r => r.CreatedByUserId).IsRequired(false);
        builder.Property(r => r.CreatedOnUtc).IsRequired();
        builder.Property(r => r.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(r => new { r.VendorId, r.Status });
        builder.HasIndex(r => r.VendorReturnId);
    }
}
