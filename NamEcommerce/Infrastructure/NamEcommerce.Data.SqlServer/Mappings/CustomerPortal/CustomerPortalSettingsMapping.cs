using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class CustomerPortalSettingsMapping : IEntityTypeConfiguration<CustomerPortalSettings>
{
    public void Configure(EntityTypeBuilder<CustomerPortalSettings> builder)
    {
        builder.ToTable(nameof(CustomerPortalSettings), DbScheme);
        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.OtpEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(settings => settings.CreatedOnUtc).IsRequired();
        builder.Property(settings => settings.UpdatedOnUtc).IsRequired(false);
        builder.Property(settings => settings.UpdatedByUserId).IsRequired(false);
    }
}
