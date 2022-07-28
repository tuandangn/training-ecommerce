namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductMapping : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(nameof(Product), NamEcommerceDataDefaults.DbScheme);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
    }
}
