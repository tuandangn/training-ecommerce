namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderItemChangeAuditMapping : IEntityTypeConfiguration<OrderItemChangeAudit>
{
    public void Configure(EntityTypeBuilder<OrderItemChangeAudit> builder)
    {
        builder.ToTable(nameof(OrderItemChangeAudit), DbScheme);
        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.ProductName).HasMaxLength(1000).IsRequired();
        builder.Property(audit => audit.Action).HasConversion<int>().IsRequired();
        builder.Property(audit => audit.OldQuantity).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.NewQuantity).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.OldUnitPrice).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.NewUnitPrice).HasColumnType("decimal(18,2)").IsRequired(false);
        builder.Property(audit => audit.ChangedByUsername).HasMaxLength(1000).IsRequired(false);
        builder.Property(audit => audit.CreatedOnUtc).IsRequired();

        builder.HasIndex(audit => audit.OrderId);
        builder.HasIndex(audit => new { audit.OrderId, audit.CreatedOnUtc });
        builder.HasIndex(audit => audit.OrderItemId);

        builder.HasOne<Order>().WithMany().HasForeignKey(audit => audit.OrderId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Product>().WithMany().HasForeignKey(audit => audit.ProductId).OnDelete(DeleteBehavior.NoAction);
    }
}
