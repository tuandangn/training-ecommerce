using NamEcommerce.Domain.Entities.CustomerPortal;

namespace NamEcommerce.Data.SqlServer.Mappings.CustomerPortal;

public sealed class DeliveryNoteAccessTokenMapping : IEntityTypeConfiguration<DeliveryNoteAccessToken>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteAccessToken> builder)
    {
        builder.ToTable(nameof(DeliveryNoteAccessToken), DbScheme);
        builder.HasKey(token => token.Id);

        builder.Property(token => token.DeliveryNoteId).IsRequired();
        builder.Property(token => token.TokenHash).HasMaxLength(500).IsRequired();
        builder.Property(token => token.ExpiresOnUtc).IsRequired(false);
        builder.Property(token => token.RevokedOnUtc).IsRequired(false);
        builder.Property(token => token.CreatedOnUtc).IsRequired();
        builder.Property(token => token.LastViewedOnUtc).IsRequired(false);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.DeliveryNoteId);
    }
}
