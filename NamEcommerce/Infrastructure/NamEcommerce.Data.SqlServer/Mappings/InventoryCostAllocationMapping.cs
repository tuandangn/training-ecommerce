namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class InventoryCostAllocationMapping : IEntityTypeConfiguration<InventoryCostAllocation>
{
    public void Configure(EntityTypeBuilder<InventoryCostAllocation> builder)
    {
        builder.ToTable(nameof(InventoryCostAllocation), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ProductId, p.CreatedAtUtc });
        builder.HasIndex(p => new { p.OutboundReferenceType, p.OutboundReferenceId, p.OutboundReferenceItemId });
        builder.HasIndex(p => p.OutboundLedgerEntryId);
        builder.HasIndex(p => p.InboundLayerId);
        builder.HasIndex(p => p.CostingStatus);
        builder.HasIndex(p => p.CostingRunId);

        builder.Property(p => p.Quantity).HasColumnType("decimal(18,4)");
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
