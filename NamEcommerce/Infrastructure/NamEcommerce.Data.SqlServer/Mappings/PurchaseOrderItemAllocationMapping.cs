namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class PurchaseOrderItemAllocationMapping : IEntityTypeConfiguration<PurchaseOrderItemAllocation>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItemAllocation> builder)
    {
        builder.ToTable(nameof(PurchaseOrderItemAllocation), DbScheme);
        builder.HasKey(allocation => allocation.Id);

        builder.ComplexProperty(c => c.PurchaseOrderItemId, purchaseOrderItemIdProp =>
        {
            purchaseOrderItemIdProp.Property(n => n.PrimaryId)
                          .HasColumnName($"{nameof(PurchaseOrder)}Id")
                          .IsRequired();
            purchaseOrderItemIdProp.Property(n => n.SecondaryId)
                          .HasColumnName(nameof(PurchaseOrderItemAllocation.PurchaseOrderItemId))
                          .IsRequired();
        });

        builder.ComplexProperty(c => c.OrderItemId, orderItemIdProp =>
        {
            orderItemIdProp.Property(n => n.PrimaryId)
                          .HasColumnName($"{nameof(Order)}Id")
                          .IsRequired();
            orderItemIdProp.Property(n => n.SecondaryId)
                          .HasColumnName(nameof(PurchaseOrderItemAllocation.OrderItemId))
                          .IsRequired();
        });

        builder.Property(allocation => allocation.AllocatedQuantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(allocation => allocation.ReceivedQuantity).HasColumnType("decimal(18,2)").IsRequired().HasDefaultValue(0m);
        builder.Property(allocation => allocation.Status).IsRequired().HasConversion<int>();
        builder.Property(allocation => allocation.IsDirectShip).IsRequired().HasDefaultValue(false);
        builder.Property(allocation => allocation.DirectShipAddress).HasMaxLength(500).IsRequired(false);
        builder.Property(allocation => allocation.DirectShipContactName).HasMaxLength(200).IsRequired(false);
        builder.Property(allocation => allocation.DirectShipContactPhone).HasMaxLength(50).IsRequired(false);
        builder.Property(allocation => allocation.DirectShipPriority).IsRequired().HasDefaultValue(0);
        builder.Property(allocation => allocation.CreatedOnUtc).IsRequired();
    }
}
