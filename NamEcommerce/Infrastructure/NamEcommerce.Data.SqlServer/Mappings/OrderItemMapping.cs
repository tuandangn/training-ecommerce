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

        builder.HasOne<Product>().WithMany().HasForeignKey(oi => oi.ProductId);
    }
}
