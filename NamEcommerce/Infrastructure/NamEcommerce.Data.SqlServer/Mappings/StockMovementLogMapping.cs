namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class StockMovementLogMapping : IEntityTypeConfiguration<StockMovementLog>
{
    public void Configure(EntityTypeBuilder<StockMovementLog> builder)
    {
        builder.ToTable(nameof(StockMovementLog), DbScheme);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Quantity).HasColumnType("decimal(18,2)");
        builder.Property(p => p.QuantityBefore).HasColumnType("decimal(18,2)");
        builder.Property(p => p.QuantityAfter).HasColumnType("decimal(18,2)");
        
        builder.Property(p => p.Note).HasMaxLength(1000);
    }
}
