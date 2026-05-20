using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerPaymentIntentMapping : IEntityTypeConfiguration<CustomerPaymentIntent>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentIntent> builder)
    {
        builder.ToTable(nameof(CustomerPaymentIntent), DbScheme);
        builder.HasKey(intent => intent.Id);

        builder.Property(intent => intent.CustomerId).IsRequired();
        builder.Property(intent => intent.CustomerDebtId).IsRequired(false);
        builder.Property(intent => intent.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(intent => intent.Provider).HasMaxLength(100).IsRequired();
        builder.Property(intent => intent.ProviderIntentId).HasMaxLength(200).IsRequired(false);
        builder.Property(intent => intent.Status).HasConversion<int>().IsRequired();
        builder.Property(intent => intent.FailureReason).HasMaxLength(1000).IsRequired(false);
        builder.Property(intent => intent.CreatedOnUtc).IsRequired();
        builder.Property(intent => intent.CompletedOnUtc).IsRequired(false);
        builder.Property(intent => intent.ReconciledOnUtc).IsRequired(false);
        builder.Property(intent => intent.ReconciledByUserId).IsRequired(false);
        builder.Property(intent => intent.CustomerPaymentId).IsRequired(false);

        builder.HasIndex(intent => new { intent.CustomerId, intent.CreatedOnUtc });
        builder.HasIndex(intent => intent.ProviderIntentId);
        builder.HasIndex(intent => intent.Status);
    }
}
