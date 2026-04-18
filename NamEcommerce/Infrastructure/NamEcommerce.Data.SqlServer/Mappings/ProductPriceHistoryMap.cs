namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductPriceHistoryMap : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
    {
        builder.ToTable(nameof(ProductPriceHistory), DbScheme);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductId).IsRequired();
        builder.Property(p => p.OldPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.NewPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.OldCostPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.NewCostPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Note).HasMaxLength(800);
        builder.Property(p => p.CreatedOnUtc).IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
