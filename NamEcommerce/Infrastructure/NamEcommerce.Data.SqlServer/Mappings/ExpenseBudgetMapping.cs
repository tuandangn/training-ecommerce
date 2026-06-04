using NamEcommerce.Domain.Entities.Finance;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ExpenseBudgetMapping : IEntityTypeConfiguration<ExpenseBudget>
{
    public void Configure(EntityTypeBuilder<ExpenseBudget> builder)
    {
        builder.ToTable("ExpenseBudgets", DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExpenseType).IsRequired();
        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.ModifiedOnUtc);

        builder.HasIndex(x => new { x.ExpenseType, x.Year, x.Month }).IsUnique();
    }
}
