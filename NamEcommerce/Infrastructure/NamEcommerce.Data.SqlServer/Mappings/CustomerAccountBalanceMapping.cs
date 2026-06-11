using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerAccountBalanceMapping : IEntityTypeConfiguration<CustomerAccountBalance>
{
    public void Configure(EntityTypeBuilder<CustomerAccountBalance> builder)
    {
        builder.ToTable(nameof(CustomerAccountBalance), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId).IsUnique();

        builder.Property(x => x.Balance).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.LastEntryOnUtc).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
    }
}
