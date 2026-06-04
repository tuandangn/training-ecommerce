using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class BankTransferVerificationLogMapping : IEntityTypeConfiguration<BankTransferVerificationLog>
{
    public void Configure(EntityTypeBuilder<BankTransferVerificationLog> builder)
    {
        builder.ToTable("BankTransferVerificationLogs", DbScheme);
        builder.HasKey(log => log.Id);

        builder.Property(log => log.ReferenceCode).HasMaxLength(25).IsRequired();
        builder.Property(log => log.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(log => log.BankId).HasMaxLength(50).IsRequired();
        builder.Property(log => log.AccountNo).HasMaxLength(50).IsRequired();
        builder.Property(log => log.ProviderTransactionId).HasMaxLength(100).IsRequired();
        builder.Property(log => log.RawPayload).HasMaxLength(4000).IsRequired(false);
        builder.Property(log => log.ErrorMessage).HasMaxLength(500).IsRequired(false);
        builder.Property(log => log.Source).HasConversion<int>().IsRequired();
        builder.Property(log => log.Status).HasConversion<int>().IsRequired();
        builder.Property(log => log.PaymentIntentId).IsRequired(false);
        builder.Property(log => log.ProviderConfirmedAtUtc).IsRequired();
        builder.Property(log => log.CreatedOnUtc).IsRequired();
        builder.Property(log => log.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(log => log.ReferenceCode);
        builder.HasIndex(log => log.ProviderTransactionId);
        builder.HasIndex(log => log.PaymentIntentId);
    }
}
