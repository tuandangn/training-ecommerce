using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class BankTransferPaymentIntentMapping : IEntityTypeConfiguration<BankTransferPaymentIntent>
{
    public void Configure(EntityTypeBuilder<BankTransferPaymentIntent> builder)
    {
        builder.ToTable(nameof(BankTransferPaymentIntent), DbScheme);
        builder.HasKey(intent => intent.Id);

        builder.Property(intent => intent.ReferenceCode).HasMaxLength(25).IsRequired();
        builder.Property(intent => intent.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(intent => intent.CustomerId).IsRequired(false);
        builder.Property(intent => intent.BankId).HasMaxLength(20).IsRequired();
        builder.Property(intent => intent.AccountNo).HasMaxLength(50).IsRequired();
        builder.Property(intent => intent.AccountName).HasMaxLength(255).IsRequired();
        builder.Property(intent => intent.Template).HasMaxLength(50).IsRequired();
        builder.Property(intent => intent.QrImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(intent => intent.Status).HasConversion<int>().IsRequired();
        builder.Property(intent => intent.Note).HasMaxLength(1000).IsRequired(false);
        builder.Property(intent => intent.OrderId).IsRequired(false);
        builder.Property(intent => intent.DeliveryNoteId).IsRequired(false);
        builder.Property(intent => intent.CustomerDebtId).IsRequired(false);
        builder.Property(intent => intent.CustomerPaymentId).IsRequired(false);
        builder.Property(intent => intent.VerificationSource).HasConversion<int?>().IsRequired(false);
        builder.Property(intent => intent.ProviderTransactionId).HasMaxLength(100).IsRequired(false);
        builder.Property(intent => intent.RawPayload).IsRequired(false);
        builder.Property(intent => intent.VerifiedAtUtc).IsRequired(false);
        builder.Property(intent => intent.VerifiedByUserId).IsRequired(false);
        builder.Property(intent => intent.CreatedOnUtc).IsRequired();
        builder.Property(intent => intent.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(intent => intent.ReferenceCode).IsUnique();
        builder.HasIndex(intent => new { intent.Status, intent.CreatedOnUtc });
        builder.HasIndex(intent => intent.CustomerId);
        builder.HasIndex(intent => intent.ProviderTransactionId);
        builder.HasIndex(intent => intent.OrderId);
        builder.HasIndex(intent => intent.DeliveryNoteId);
        builder.HasIndex(intent => intent.CustomerDebtId);
        builder.HasIndex(intent => intent.CustomerPaymentId);
    }
}
