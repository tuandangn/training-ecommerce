namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class InventoryCostLedgerEntryMapping : IEntityTypeConfiguration<InventoryCostLedgerEntry>
{
    public void Configure(EntityTypeBuilder<InventoryCostLedgerEntry> builder)
    {
        builder.ToTable(nameof(InventoryCostLedgerEntry), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ProductId, p.OccurredAtUtc, p.SequenceNumber });
        builder.HasIndex(p => new { p.ReferenceType, p.ReferenceId, p.ReferenceItemId });
        builder.HasIndex(p => p.CostingStatus);
        builder.HasIndex(p => p.CostingRunId);

        builder.Property(p => p.QuantityDelta).HasColumnType("decimal(18,4)");
        builder.Property(p => p.UnitCost).HasColumnType("decimal(18,4)");
        builder.Property(p => p.TotalCost).HasColumnType("decimal(18,4)");
        builder.Property(p => p.QuantityBalanceAfter).HasColumnType("decimal(18,4)");
        builder.Property(p => p.ValueBalanceAfter).HasColumnType("decimal(18,4)");
        builder.Property(p => p.AverageCostAfter).HasColumnType("decimal(18,4)");

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
