using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class VendorCreditNoteAllocationMapping : IEntityTypeConfiguration<VendorCreditNoteAllocation>
{
    public void Configure(EntityTypeBuilder<VendorCreditNoteAllocation> builder)
    {
        builder.ToTable(nameof(VendorCreditNoteAllocation), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VendorCreditNoteId).IsRequired();
        builder.HasIndex(x => x.VendorCreditNoteId);
        builder.Property(x => x.VendorCreditNoteCode).IsRequired().HasMaxLength(100);

        builder.Property(x => x.SourceReturnId).IsRequired();
        builder.HasIndex(x => x.SourceReturnId);
        builder.Property(x => x.SourceReturnCode).IsRequired().HasMaxLength(100);

        builder.Property(x => x.VendorDebtId).IsRequired();
        builder.HasIndex(x => x.VendorDebtId);
        builder.Property(x => x.VendorDebtCode).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.AppliedOnUtc).IsRequired();
        builder.Property(x => x.AppliedByUserId).IsRequired(false);
        builder.Property(x => x.ReversedOnUtc).IsRequired(false);
        builder.Property(x => x.ReversedByUserId).IsRequired(false);
        builder.Property(x => x.ReverseReason).IsRequired(false).HasMaxLength(1000);
        builder.Ignore(x => x.IsReversed);
    }
}
