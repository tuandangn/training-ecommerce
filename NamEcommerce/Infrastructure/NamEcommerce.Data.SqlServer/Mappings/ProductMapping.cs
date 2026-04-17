namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductMapping : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(nameof(Product), DbScheme);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.NormalizedName).HasMaxLength(400);
        builder.Property(p => p.ShortDesc).HasMaxLength(800).IsRequired();
        builder.Property(p => p.NormalizedShortDesc).HasMaxLength(1600);
        builder.Property(p => p.CostPrice).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(p => p.CreatedOnUtc).IsRequired();
        builder.Property(p => p.UpdatedOnUtc);

        builder.Navigation(p => p.ProductCategories).AutoInclude();
        builder.Navigation(p => p.ProductPictures).AutoInclude();
    }
}
