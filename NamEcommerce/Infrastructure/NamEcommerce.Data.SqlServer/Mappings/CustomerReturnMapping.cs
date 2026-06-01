using NamEcommerce.Domain.Entities.Returns;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerReturnMapping : IEntityTypeConfiguration<CustomerReturn>
{
    public void Configure(EntityTypeBuilder<CustomerReturn> builder)
    {
        builder.ToTable(nameof(CustomerReturn), DbScheme);
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.DeliveryNoteId).IsRequired();
        builder.Property(r => r.DeliveryNoteCode).IsRequired().HasMaxLength(50);

        builder.Property(r => r.CustomerId).IsRequired();
        builder.Property(r => r.CustomerName).IsRequired().HasMaxLength(500);

        builder.Property(r => r.WarehouseId).IsRequired(false);
        builder.Property(r => r.WarehouseName).IsRequired(false).HasMaxLength(500);

        builder.Property(r => r.Note).HasMaxLength(2000);
        builder.Property(r => r.Status).IsRequired().HasConversion<int>();
        builder.Property(r => r.ReturnDate).IsRequired();
        builder.Property(r => r.ConfirmedOnUtc).IsRequired(false);

        builder.Property(r => r.AdditionalCost).IsRequired().HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        builder.Property(r => r.CompensateInNextDelivery).IsRequired().HasDefaultValue(false);

        builder.Property(r => r.GeneratedGoodsReceiptId).IsRequired(false);

        builder.Property(r => r.CreatedByUserId).IsRequired(false);
        builder.Property(r => r.CreatedOnUtc).IsRequired();
        builder.Property(r => r.UpdatedOnUtc).IsRequired(false);

        builder.HasIndex(r => new { r.DeliveryNoteId, r.Status });
        builder.HasIndex(r => new { r.CustomerId, r.Status });

        builder.HasMany(r => r.Items)
            .WithOne()
            .HasForeignKey(i => i.CustomerReturnId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Items).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
