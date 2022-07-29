namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductCategoryMapping : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable(nameof(ProductCategory), DbScheme);

        builder.HasKey(pc => pc.Id);

        builder.HasOne<Product>().WithMany(p => p.ProductCategories).HasForeignKey(pc => pc.ProductId);
        builder.HasOne<Category>().WithMany().HasForeignKey(pc => pc.CategoryId);
    }
}
