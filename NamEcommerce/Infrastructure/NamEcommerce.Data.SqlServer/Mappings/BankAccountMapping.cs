using NamEcommerce.Domain.Entities.Finance;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class BankAccountMapping : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable(nameof(BankAccount), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BankCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AccountNumber).IsRequired().HasMaxLength(30);
        builder.Property(x => x.AccountHolderName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.OpeningBalance).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.IsDefault).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
    }
}
