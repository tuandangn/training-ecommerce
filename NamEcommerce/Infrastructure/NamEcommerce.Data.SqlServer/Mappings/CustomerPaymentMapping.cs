using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerPaymentMapping : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.ToTable("CustomerPayment", DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(255);
        
        builder.Property(x => x.OrderId).IsRequired(false);
        builder.Property(x => x.OrderCode).IsRequired(false).HasMaxLength(100);
        
        builder.Property(x => x.DeliveryNoteId).IsRequired(false);
        builder.Property(x => x.DeliveryNoteCode).IsRequired(false).HasMaxLength(100);
        
        builder.Property(x => x.CustomerDebtId).IsRequired(false);

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
