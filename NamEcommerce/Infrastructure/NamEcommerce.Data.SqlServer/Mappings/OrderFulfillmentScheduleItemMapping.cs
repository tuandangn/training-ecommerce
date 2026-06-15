using NamEcommerce.Domain.Entities.Orders;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderFulfillmentScheduleItemMapping : IEntityTypeConfiguration<OrderFulfillmentScheduleItem>
{
    public void Configure(EntityTypeBuilder<OrderFulfillmentScheduleItem> builder)
    {
        builder.ToTable(nameof(OrderFulfillmentScheduleItem), DbScheme);
        builder.HasKey(item => item.Id);

        builder.Property(item => item.OrderFulfillmentScheduleId).IsRequired();
        builder.Property(item => item.OrderItemId).IsRequired();
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductName).IsRequired().HasMaxLength(500);
        builder.Property(item => item.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(item => item.CreatedOnUtc).IsRequired();

        builder.HasIndex(item => item.OrderFulfillmentScheduleId);
        builder.HasIndex(item => item.OrderItemId);
        builder.HasIndex(item => item.ProductId);

        builder.HasOne<OrderItem>().WithMany().HasForeignKey(item => item.OrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
