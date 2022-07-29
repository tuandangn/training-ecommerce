namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderItemMapping : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(nameof(OrderItem), DbScheme);

        builder.HasKey(oi => oi.Id);

        builder.HasOne<Product>().WithMany().HasForeignKey(oi => oi.ProductId);
    }
}
