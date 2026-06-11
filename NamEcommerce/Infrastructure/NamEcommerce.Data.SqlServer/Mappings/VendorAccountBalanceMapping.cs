using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorAccountBalanceMapping : IEntityTypeConfiguration<VendorAccountBalance>
{
    public void Configure(EntityTypeBuilder<VendorAccountBalance> builder)
    {
        builder.ToTable(nameof(VendorAccountBalance), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VendorId).IsRequired();
        builder.HasIndex(x => x.VendorId).IsUnique();

        builder.Property(x => x.Balance).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.LastEntryOnUtc).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
    }
}
