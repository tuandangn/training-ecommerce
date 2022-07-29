namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductMapping : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(nameof(Product), DbScheme);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ShortDesc).HasMaxLength(800).IsRequired();
    }
}
