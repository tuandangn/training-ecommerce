using NamEcommerce.Domain.Entities.Debts;
using static NamEcommerce.Data.SqlServer.NamEcommerceEfDataDefaults;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerCreditNoteMapping : IEntityTypeConfiguration<CustomerCreditNote>
{
    public void Configure(EntityTypeBuilder<CustomerCreditNote> builder)
    {
        builder.ToTable(nameof(CustomerCreditNote), DbScheme);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId);
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(255);

        builder.Property(x => x.SourceType).IsRequired().HasConversion<int>();
        builder.Property(x => x.SourceReturnId).IsRequired();
        builder.HasIndex(x => x.SourceReturnId);
        builder.Property(x => x.SourceReturnCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SourceDeliveryNoteId).IsRequired(false);
        builder.HasIndex(x => x.SourceDeliveryNoteId);

        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.AppliedAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.RemainingAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();

        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired(false);
        builder.Property(x => x.CancelledOnUtc).IsRequired(false);

        builder.HasMany(x => x.Allocations)
            .WithOne()
            .HasForeignKey(x => x.CustomerCreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
