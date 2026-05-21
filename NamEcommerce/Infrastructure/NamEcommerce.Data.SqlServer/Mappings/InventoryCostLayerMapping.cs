namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class InventoryCostLayerMapping : IEntityTypeConfiguration<InventoryCostLayer>
{
    public void Configure(EntityTypeBuilder<InventoryCostLayer> builder)
    {
        builder.ToTable(nameof(InventoryCostLayer), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ProductId, p.OpenedAtUtc });
        builder.HasIndex(p => new { p.SourceReferenceType, p.SourceReferenceId, p.SourceReferenceItemId });
        builder.HasIndex(p => p.CostingStatus);
        builder.HasIndex(p => p.CostingRunId);

        builder.Property(p => p.OriginalQuantity).HasColumnType("decimal(18,4)");
        builder.Property(p => p.RemainingQuantity).HasColumnType("decimal(18,4)");
        builder.Property(p => p.UnitCost).HasColumnType("decimal(18,4)");
        builder.Property(p => p.TotalCost).HasColumnType("decimal(18,4)");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(p => p.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
