using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerCreditNoteAllocationMapping : IEntityTypeConfiguration<CustomerCreditNoteAllocation>
{
    public void Configure(EntityTypeBuilder<CustomerCreditNoteAllocation> builder)
    {
        builder.ToTable(nameof(CustomerCreditNoteAllocation), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerCreditNoteId).IsRequired();
        builder.HasIndex(x => x.CustomerCreditNoteId);
        builder.Property(x => x.CustomerCreditNoteCode).IsRequired().HasMaxLength(100);

        builder.Property(x => x.SourceReturnId).IsRequired();
        builder.HasIndex(x => x.SourceReturnId);
        builder.Property(x => x.SourceReturnCode).IsRequired().HasMaxLength(100);

        builder.Property(x => x.CustomerDebtId).IsRequired();
        builder.HasIndex(x => x.CustomerDebtId);
        builder.Property(x => x.CustomerDebtCode).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.AppliedOnUtc).IsRequired();
        builder.Property(x => x.AppliedByUserId).IsRequired(false);
        builder.Property(x => x.ReversedOnUtc).IsRequired(false);
        builder.Property(x => x.ReversedByUserId).IsRequired(false);
        builder.Property(x => x.ReverseReason).IsRequired(false).HasMaxLength(1000);
        builder.Ignore(x => x.IsReversed);
    }
}
