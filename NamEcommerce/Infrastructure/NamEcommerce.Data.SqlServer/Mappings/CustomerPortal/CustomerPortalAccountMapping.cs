using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerPortalAccountMapping : IEntityTypeConfiguration<CustomerPortalAccount>
{
    public void Configure(EntityTypeBuilder<CustomerPortalAccount> builder)
    {
        builder.ToTable(nameof(CustomerPortalAccount), DbScheme);
        builder.HasKey(account => account.Id);

        builder.Property(account => account.CustomerId).IsRequired();
        builder.HasIndex(account => account.CustomerId).IsUnique();

        builder.Property(account => account.PasswordHash).HasMaxLength(500);
        builder.Property(account => account.PasswordSalt).HasMaxLength(500);
        builder.Property(account => account.Status).HasConversion<int>().IsRequired();
        builder.Property(account => account.CreatedOnUtc).IsRequired();
        builder.Property(account => account.UpdatedOnUtc).IsRequired(false);
        builder.Property(account => account.PasswordSetOnUtc).IsRequired(false);
        builder.Property(account => account.LastLoginOnUtc).IsRequired(false);
        builder.Property(account => account.LastKnownLatitude).IsRequired(false);
        builder.Property(account => account.LastKnownLongitude).IsRequired(false);
        builder.Property(account => account.LastKnownLocationAccuracyMeters).IsRequired(false);
        builder.Property(account => account.LastKnownLocationCapturedOnUtc).IsRequired(false);
        builder.Property(account => account.LastKnownLocationSource).HasMaxLength(50);
    }
}
