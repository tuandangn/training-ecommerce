using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerReturnRequestItemPictureMapping : IEntityTypeConfiguration<CustomerReturnRequestItemPicture>
{
    public void Configure(EntityTypeBuilder<CustomerReturnRequestItemPicture> builder)
    {
        builder.ToTable(nameof(CustomerReturnRequestItemPicture), DbScheme);
        builder.HasKey(picture => picture.Id);

        builder.Property(picture => picture.CustomerReturnRequestItemId).IsRequired();
        builder.Property(picture => picture.PictureId).IsRequired();
        builder.Property(picture => picture.CreatedOnUtc).IsRequired();

        builder.HasIndex(picture => picture.CustomerReturnRequestItemId);
        builder.HasIndex(picture => picture.PictureId);
    }
}
