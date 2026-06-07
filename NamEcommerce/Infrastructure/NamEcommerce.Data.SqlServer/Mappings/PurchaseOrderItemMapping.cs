using NamEcommerce.Domain.Entities.PurchaseOrders;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class PurchaseOrderItemMapping : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable(nameof(PurchaseOrderItem), DbScheme);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.QuantityOrdered).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.UnitCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.QuantityReceived).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.TaxRate).HasColumnType("decimal(5,4)").IsRequired(false);
        builder.Property(p => p.TaxAmount).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
        builder.Property(p => p.Note).HasMaxLength(1000);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
