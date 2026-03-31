namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorMapping : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable(nameof(Vendor), DbScheme);
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.NormalizedName).HasMaxLength(400);
        builder.Property(v => v.PhoneNumber).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Address).HasMaxLength(400);
        builder.Property(v => v.NormalizedAddress).HasMaxLength(800);
        builder.Property(v => v.DisplayOrder).IsRequired();
    }
}
