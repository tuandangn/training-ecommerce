namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class PictureMapping : IEntityTypeConfiguration<Picture>
{
    public void Configure(EntityTypeBuilder<Picture> builder)
    {
        builder.ToTable(nameof(Picture), DbScheme);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Extension).HasMaxLength(100).IsRequired();
        builder.Property(p => p.SeoName).HasMaxLength(400);
    }
}
