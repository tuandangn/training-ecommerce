namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class ProductPictureMapping : IEntityTypeConfiguration<ProductPicture>
{
    public void Configure(EntityTypeBuilder<ProductPicture> builder)
    {
        builder.ToTable(nameof(ProductPicture), DbScheme);

        builder.HasKey(p => p.Id);

        builder.HasOne<Product>().WithMany(p => p.ProductPictures).HasForeignKey(pp => pp.ProductId);
        builder.HasOne<Picture>().WithMany().HasForeignKey(pp => pp.PictureId);
    }
}
