namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class StockReservationEntryMapping : IEntityTypeConfiguration<StockReservationEntry>
{
    public void Configure(EntityTypeBuilder<StockReservationEntry> builder)
    {
        builder.ToTable(nameof(StockReservationEntry), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.ProductId, p.WarehouseId });
        builder.HasIndex(p => p.ReferenceId);

        builder.Property(p => p.ProductId).IsRequired();
        builder.Property(p => p.WarehouseId).IsRequired();
        builder.Property(p => p.QuantityDelta).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.ReferenceId).IsRequired();
        builder.Property(p => p.Note).HasMaxLength(500);
        builder.Property(p => p.CreatedOnUtc).IsRequired();
    }
}
