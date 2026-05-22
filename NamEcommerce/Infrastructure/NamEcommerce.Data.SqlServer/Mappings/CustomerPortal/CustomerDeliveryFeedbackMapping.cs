using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerDeliveryFeedbackMapping : IEntityTypeConfiguration<CustomerDeliveryFeedback>
{
    public void Configure(EntityTypeBuilder<CustomerDeliveryFeedback> builder)
    {
        builder.ToTable(nameof(CustomerDeliveryFeedback), DbScheme);
        builder.HasKey(feedback => feedback.Id);

        builder.Property(feedback => feedback.CustomerId).IsRequired();
        builder.Property(feedback => feedback.DeliveryNoteId).IsRequired();
        builder.Property(feedback => feedback.Rating).IsRequired(false);
        builder.Property(feedback => feedback.Message).HasMaxLength(2000).IsRequired(false);
        builder.Property(feedback => feedback.Status).HasConversion<int>().IsRequired();
        builder.Property(feedback => feedback.CreatedOnUtc).IsRequired();
        builder.Property(feedback => feedback.ReviewedOnUtc).IsRequired(false);

        builder.HasIndex(feedback => new { feedback.CustomerId, feedback.DeliveryNoteId });
    }
}
