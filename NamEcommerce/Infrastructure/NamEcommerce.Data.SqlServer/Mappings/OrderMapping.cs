using NamEcommerce.Domain.Entities.Customers;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderMapping : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(nameof(Order), DbScheme);
        builder.HasKey(o => o.Id);

        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(o => o.ShippingAddress).HasMaxLength(1000);
        builder.Property(o => o.NormalizedShippingAddress).HasMaxLength(1000);

        builder.Property(o => o.LockOrderReason).HasMaxLength(1000);

        builder.Property(p => p.OrderSubTotal);
        builder.Property(p => p.OrderTotal);

        builder.Property(p => p.CreatedByUserId);
        builder.Property(p => p.CreatedOnUtc);
        builder.Property(p => p.ExpectedShippingDateUtc);
        builder.Property(p => p.OrderDiscount);

        builder.Property(o => o.CustomerName).HasMaxLength(1000);
        builder.Property(o => o.CustomerPhone).HasMaxLength(1000);
        builder.Property(o => o.CustomerAddress).HasMaxLength(1000);
        builder.Property(o => o.CreatedByUsername).HasMaxLength(1000);

        builder.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId);
        builder.HasMany(o => o.OrderItems).WithOne().HasForeignKey(oi => oi.OrderId);

        builder.Navigation(o => o.OrderItems).AutoInclude();
    }
}
