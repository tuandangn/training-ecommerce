namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductReservationLedgerMapping : IEntityTypeConfiguration<ProductReservationLedger>
{
    public void Configure(EntityTypeBuilder<ProductReservationLedger> builder)
    {
        builder.ToTable(nameof(ProductReservationLedger), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => new { p.ProductId, p.OrderId });
        builder.HasIndex(p => p.ReferenceId);

        builder.Property(p => p.ProductId).IsRequired();
        builder.Property(p => p.OrderId).IsRequired();
        builder.Property(p => p.QuantityDelta).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(p => p.Reason).IsRequired();
        builder.Property(p => p.CreatedOnUtc).IsRequired();
    }
}
