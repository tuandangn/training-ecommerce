namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class InventoryCostRebuildRunMapping : IEntityTypeConfiguration<InventoryCostRebuildRun>
{
    public void Configure(EntityTypeBuilder<InventoryCostRebuildRun> builder)
    {
        builder.ToTable(nameof(InventoryCostRebuildRun), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.ProductId, p.StartedAtUtc });

        builder.Property(p => p.ErrorMessage).HasMaxLength(4000);
    }
}
