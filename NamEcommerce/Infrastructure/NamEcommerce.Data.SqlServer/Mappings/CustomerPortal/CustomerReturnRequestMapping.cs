using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerReturnRequestMapping : IEntityTypeConfiguration<CustomerReturnRequest>
{
    public void Configure(EntityTypeBuilder<CustomerReturnRequest> builder)
    {
        builder.ToTable(nameof(CustomerReturnRequest), DbScheme);
        builder.HasKey(request => request.Id);

        builder.Property(request => request.CustomerId).IsRequired();
        builder.Property(request => request.DeliveryNoteId).IsRequired();
        builder.Property(request => request.Status).HasConversion<int>().IsRequired();
        builder.Property(request => request.Reason).HasMaxLength(2000).IsRequired(false);
        builder.Property(request => request.CompensateInNextDelivery).IsRequired().HasDefaultValue(false);
        builder.Property(request => request.AdminNote).HasMaxLength(2000).IsRequired(false);
        builder.Property(request => request.CreatedOnUtc).IsRequired();
        builder.Property(request => request.ReviewedOnUtc).IsRequired(false);
        builder.Property(request => request.ReviewedByUserId).IsRequired(false);
        builder.Property(request => request.ConvertedCustomerReturnId).IsRequired(false);

        builder.HasIndex(request => new { request.CustomerId, request.CreatedOnUtc });
        builder.HasIndex(request => new { request.DeliveryNoteId, request.Status });

        builder.Metadata.FindNavigation(nameof(CustomerReturnRequest.Items))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(request => request.Items).WithOne().HasForeignKey(item => item.CustomerReturnRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.Items).AutoInclude();
    }
}
