using NamEcommerce.Domain.Entities.DeliveryNotes;

namespace NamEcommerce.Data.SqlServer.Mappings;

public class DeliveryNoteItemMap : IEntityTypeConfiguration<DeliveryNoteItem>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteItem> builder)
    {
        builder.ToTable(nameof(DeliveryNoteItem), DbScheme);
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeliveryNoteId).IsRequired();

        builder.Property(d => d.OrderItemId).IsRequired();
        builder.Property(d => d.ProductId).IsRequired();
        builder.Property(d => d.WarehouseId).IsRequired();
        builder.Property(d => d.ProductName).HasMaxLength(400).IsRequired();
        builder.Property(d => d.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(d => d.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.HasIndex(d => d.WarehouseId);

        builder.Property(d => d.DiscountPercent).HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(0m);
        builder.Property(d => d.DiscountAmount).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
        builder.Property(d => d.TaxRate).HasColumnType("decimal(5,4)").IsRequired(false);
        builder.Property(d => d.TaxAmount).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);

        // Computed — không persist
        builder.Ignore(d => d.SubTotal);
        builder.Ignore(d => d.NetAmount);
    }
}
