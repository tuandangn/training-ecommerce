using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerDebtMapping : IEntityTypeConfiguration<CustomerDebt>
{
    public void Configure(EntityTypeBuilder<CustomerDebt> builder)
    {
        builder.ToTable(nameof(CustomerDebt), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.NormalizedCustomerName).HasMaxLength(400);
        builder.Property(x => x.CustomerPhone).HasMaxLength(50);
        builder.Property(x => x.NormalizedCustomerPhone).HasMaxLength(100);
        builder.Property(x => x.CustomerAddress).HasMaxLength(500);
        builder.Property(x => x.NormalizedCustomerAddress).HasMaxLength(1000);
        
        builder.Property(x => x.DeliveryNoteId).IsRequired();
        builder.Property(x => x.DeliveryNoteCode).IsRequired().HasMaxLength(100);
        
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.OrderCode).IsRequired().HasMaxLength(100);

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
