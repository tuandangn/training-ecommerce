using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerLedgerEntryMapping : IEntityTypeConfiguration<CustomerLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CustomerLedgerEntry> builder)
    {
        builder.ToTable(nameof(CustomerLedgerEntry), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId);

        builder.Property(x => x.EntryType).IsRequired();
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReferenceType).IsRequired();
        builder.Property(x => x.ReferenceId).IsRequired(false);
        builder.Property(x => x.ReferenceCode).IsRequired(false).HasMaxLength(100);
        builder.Property(x => x.Note).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired(false);
        builder.Property(x => x.CreatedOnUtc).IsRequired();

        builder.HasIndex(x => new { x.CustomerId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.CustomerId, x.EntryType, x.ReferenceId })
            .HasFilter("[ReferenceId] IS NOT NULL");
    }
}
