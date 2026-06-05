using NamEcommerce.Domain.Entities.Finance;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class FixedAssetMapping : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable(nameof(FixedAsset), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AcquisitionCost).HasColumnType("decimal(18,0)").IsRequired();
        builder.Property(x => x.ResidualValue).HasColumnType("decimal(18,0)").IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.Category).HasConversion<int>().IsRequired();
        builder.Property(x => x.CostCenter).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.VendorInvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
        builder.Property(x => x.DisposedOnUtc).IsRequired(false);

        builder.Ignore(x => x.MonthlyDepreciation);
        builder.Ignore(x => x.DepreciationStartDate);
    }
}
