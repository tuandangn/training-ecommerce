using NamEcommerce.Domain.Entities.Finance;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ExpenseMapping : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable(nameof(Expense), DbScheme);
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Title).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.ExpenseType).IsRequired();
        builder.Property(x => x.IncurredDate).IsRequired();

        builder.Property(x => x.TaxRate).HasColumnType("decimal(5,4)").IsRequired(false);
        builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
        builder.Ignore(x => x.AmountExcludingTax);

        builder.Property(x => x.PaymentMethod).IsRequired(false).HasConversion<int>();
        builder.Property(x => x.BankAccountId).IsRequired(false);
        
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.ModifiedOnUtc);
        
        builder.Property(x => x.RecordedByUserId);

        builder.Property(x => x.ReferenceId);
        builder.Property(x => x.ReferenceType);
        builder.Property(x => x.ReferenceCode).HasMaxLength(50).IsRequired(false);
    }
}
