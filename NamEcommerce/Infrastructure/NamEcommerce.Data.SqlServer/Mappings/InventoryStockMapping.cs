namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class InventoryStockMapping : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable(nameof(InventoryStock), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ProductId, p.WarehouseId, p.WarehouseZoneId }).IsUnique();

        builder.Property(p => p.QuantityOnHand).HasColumnType("decimal(18,2)");
        builder.Property(p => p.QuantityReserved).HasColumnType("decimal(18,2)");
        builder.Property(p => p.ReorderLevel).HasColumnType("decimal(18,2)");
        builder.Property(p => p.MaxStockLevel).HasColumnType("decimal(18,2)");
    }
}
