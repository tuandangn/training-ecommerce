namespace NamEcommerce.Data.SqlServer.Mappings;

public class ProductVendorMapping : IEntityTypeConfiguration<ProductVendor>
{
    public void Configure(EntityTypeBuilder<ProductVendor> builder)
    {
        builder.ToTable(nameof(ProductVendor), DbScheme);

        builder.HasKey(mapping => new { mapping.ProductId, mapping.VendorId });

        builder.HasOne<Product>()
            .WithMany(p => p.ProductVendors)
            .HasForeignKey(mapping => mapping.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(mapping => mapping.VendorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
