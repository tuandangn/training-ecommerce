namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class InventoryCostingPolicyMapping : IEntityTypeConfiguration<InventoryCostingPolicy>
{
    public void Configure(EntityTypeBuilder<InventoryCostingPolicy> builder)
    {
        builder.ToTable(nameof(InventoryCostingPolicy), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.IsActive, p.EffectiveFromUtc });

        builder.Property(p => p.CostingMethod).IsRequired();
        builder.Property(p => p.ValuationScope).IsRequired();
        builder.Property(p => p.EffectiveFromUtc).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(1000);
    }
}
