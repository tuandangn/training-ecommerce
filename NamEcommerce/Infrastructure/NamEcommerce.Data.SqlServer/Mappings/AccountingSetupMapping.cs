using NamEcommerce.Domain.Entities.Finance;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class AccountingSetupMapping : IEntityTypeConfiguration<AccountingSetup>
{
    public void Configure(EntityTypeBuilder<AccountingSetup> builder)
    {
        builder.ToTable(nameof(AccountingSetup), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FiscalYearStartMonth).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.FiscalYearStartDay).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.AccountingStartDate).IsRequired();
        builder.Property(x => x.OpeningCash).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.OpeningEquity).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.DefaultTaxRate).IsRequired().HasColumnType("decimal(5,4)").HasDefaultValue(0.10m);
        builder.Property(x => x.CorporateTaxProvision).IsRequired(false).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsFinalized).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.FinalizedOnUtc).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
    }
}
