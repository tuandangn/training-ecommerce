using NamEcommerce.Domain.Entities.PurchaseOrders;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class PurchaseOrderItemAllocationMapping : IEntityTypeConfiguration<PurchaseOrderItemAllocation>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItemAllocation> builder)
    {
        builder.ToTable(nameof(PurchaseOrderItemAllocation), DbScheme);
        builder.HasKey(allocation => allocation.Id);

        builder.HasIndex(allocation => allocation.PurchaseOrderItemId);
        builder.HasIndex(allocation => allocation.OrderItemId);

        builder.Property(allocation => allocation.PurchaseOrderItemId).IsRequired();
        builder.Property(allocation => allocation.OrderItemId).IsRequired();
        builder.Property(allocation => allocation.AllocatedQuantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(allocation => allocation.ReceivedQuantity).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
        builder.Property(allocation => allocation.Status).IsRequired().HasDefaultValue(0).HasConversion<int>();
        builder.Property(allocation => allocation.IsDirectShip).IsRequired().HasDefaultValue(false);
        builder.Property(allocation => allocation.DirectShipAddress).HasMaxLength(500).IsRequired(false);
        builder.Property(allocation => allocation.DirectShipContactName).HasMaxLength(200).IsRequired(false);
        builder.Property(allocation => allocation.DirectShipContactPhone).HasMaxLength(50).IsRequired(false);
        builder.Property(allocation => allocation.DirectShipPriority).IsRequired().HasDefaultValue(0);
        builder.Property(allocation => allocation.CreatedOnUtc).IsRequired();

        builder.HasOne<PurchaseOrderItem>()
            .WithMany()
            .HasForeignKey(allocation => allocation.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OrderItem>()
            .WithMany()
            .HasForeignKey(allocation => allocation.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
