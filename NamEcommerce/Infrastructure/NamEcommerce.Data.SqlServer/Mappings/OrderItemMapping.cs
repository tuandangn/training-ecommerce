namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderItemMapping : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(nameof(OrderItem), DbScheme);
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.CostPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(oi => oi.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(oi => oi.Discount).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(o => o.ProductName).HasMaxLength(1000);

        builder.Property(o => o.IsDelivered).IsRequired().HasDefaultValue(false);
        builder.Property(o => o.DeliveredOnUtc).IsRequired(false);
        builder.Property(o => o.DeliveryProofPictureId).IsRequired(false);

        builder.HasOne<Product>().WithMany().HasForeignKey(oi => oi.ProductId);
    }
}
