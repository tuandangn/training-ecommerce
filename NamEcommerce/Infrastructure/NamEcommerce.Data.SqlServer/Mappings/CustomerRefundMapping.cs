using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerRefundMapping : IEntityTypeConfiguration<CustomerRefund>
{
    public void Configure(EntityTypeBuilder<CustomerRefund> builder)
    {
        builder.ToTable(nameof(CustomerRefund), DbScheme);
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.CustomerId).IsRequired();
        builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(255);

        builder.Property(r => r.CustomerReturnId).IsRequired();
        builder.Property(r => r.CustomerReturnCode).IsRequired().HasMaxLength(50);

        builder.Property(r => r.CustomerDebtId).IsRequired(false);

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

        // Index hỗ trợ query theo khách hàng và trạng thái
        builder.HasIndex(r => new { r.CustomerId, r.Status });
        builder.HasIndex(r => r.CustomerReturnId);
    }
}
