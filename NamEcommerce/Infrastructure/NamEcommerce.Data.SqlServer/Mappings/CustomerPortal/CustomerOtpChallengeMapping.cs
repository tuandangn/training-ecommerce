using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerOtpChallengeMapping : IEntityTypeConfiguration<CustomerOtpChallenge>
{
    public void Configure(EntityTypeBuilder<CustomerOtpChallenge> builder)
    {
        builder.ToTable(nameof(CustomerOtpChallenge), DbScheme);
        builder.HasKey(challenge => challenge.Id);

        builder.Property(challenge => challenge.CustomerId).IsRequired();
        builder.Property(challenge => challenge.DeliveryNoteId).IsRequired();
        builder.Property(challenge => challenge.Channel).HasConversion<int>().IsRequired();
        builder.Property(challenge => challenge.OtpHash).HasMaxLength(500).IsRequired();
        builder.Property(challenge => challenge.ExpiresOnUtc).IsRequired();
        builder.Property(challenge => challenge.AttemptCount).IsRequired();
        builder.Property(challenge => challenge.Status).HasConversion<int>().IsRequired();
        builder.Property(challenge => challenge.RequestedIp).HasMaxLength(100).IsRequired(false);
        builder.Property(challenge => challenge.RequestedUserAgent).HasMaxLength(500).IsRequired(false);
        builder.Property(challenge => challenge.SentToMasked).HasMaxLength(200).IsRequired(false);
        builder.Property(challenge => challenge.CreatedOnUtc).IsRequired();
        builder.Property(challenge => challenge.VerifiedOnUtc).IsRequired(false);

        builder.HasIndex(challenge => new { challenge.CustomerId, challenge.DeliveryNoteId, challenge.CreatedOnUtc });
        builder.HasIndex(challenge => new { challenge.RequestedIp, challenge.CreatedOnUtc });
    }
}
