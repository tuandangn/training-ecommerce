using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerPortalSessionMapping : IEntityTypeConfiguration<CustomerPortalSession>
{
    public void Configure(EntityTypeBuilder<CustomerPortalSession> builder)
    {
        builder.ToTable(nameof(CustomerPortalSession), DbScheme);
        builder.HasKey(session => session.Id);

        builder.Property(session => session.CustomerId).IsRequired();
        builder.Property(session => session.SessionTokenHash).HasMaxLength(500).IsRequired();
        builder.Property(session => session.CreatedOnUtc).IsRequired();
        builder.Property(session => session.LastSeenOnUtc).IsRequired();
        builder.Property(session => session.ExpiresOnUtc).IsRequired();
        builder.Property(session => session.RevokedOnUtc).IsRequired(false);
        builder.Property(session => session.CreatedIp).HasMaxLength(100).IsRequired(false);
        builder.Property(session => session.UserAgent).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(session => session.SessionTokenHash).IsUnique();
        builder.HasIndex(session => new { session.CustomerId, session.ExpiresOnUtc });
    }
}
