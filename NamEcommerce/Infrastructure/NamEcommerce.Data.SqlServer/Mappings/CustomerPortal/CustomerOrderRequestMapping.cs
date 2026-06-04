using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerOrderRequestMapping : IEntityTypeConfiguration<CustomerOrderRequest>
{
    public void Configure(EntityTypeBuilder<CustomerOrderRequest> builder)
    {
        builder.ToTable(nameof(CustomerOrderRequest), DbScheme);
        builder.HasKey(request => request.Id);

        builder.Property(request => request.CustomerId).IsRequired();
        builder.Property(request => request.Code).HasMaxLength(100).IsRequired();
        builder.Property(request => request.Status).HasConversion<int>().IsRequired();
        builder.Property(request => request.ExpectedShippingDateUtc).IsRequired(false);
        builder.Property(request => request.ShippingAddress).HasMaxLength(1000).IsRequired(false);
        builder.Property(request => request.Note).HasMaxLength(2000).IsRequired(false);
        builder.Property(request => request.AdminNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(request => request.CreatedOnUtc).IsRequired();
        builder.Property(request => request.ReviewedOnUtc).IsRequired(false);
        builder.Property(request => request.ReviewedByUserId).IsRequired(false);
        builder.Property(request => request.ConvertedOrderId).IsRequired(false);

        builder.HasIndex(request => request.Code).IsUnique();
        builder.HasIndex(request => new { request.CustomerId, request.CreatedOnUtc });
        builder.HasIndex(request => request.Status);

        builder.HasMany(request => request.Items).WithOne().HasForeignKey(item => item.CustomerOrderRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.Items).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
