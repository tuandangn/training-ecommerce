namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductReservationMapping : IEntityTypeConfiguration<ProductReservation>
{
    public void Configure(EntityTypeBuilder<ProductReservation> builder)
    {
        builder.ToTable(nameof(ProductReservation), DbScheme);
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.ProductId).IsUnique();

        builder.Property(p => p.ProductId).IsRequired();
        builder.Property(p => p.TotalReservedByOrder)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);
        builder.Property(p => p.UpdatedOnUtc).IsRequired();
    }
}
