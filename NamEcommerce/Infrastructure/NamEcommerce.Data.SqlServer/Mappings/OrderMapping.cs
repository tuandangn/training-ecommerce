using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Metadata;
using NamEcommerce.Domain.Values;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderMapping : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(nameof(Order), DbScheme);
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(o => o.Code).IsUnique();

        builder.ComplexProperty(c => c.ShippingAddress, shippingAddressProp =>
        {
            shippingAddressProp.Property(n => n.Value)
                          .HasColumnName(nameof(Order.ShippingAddress))
                          .HasMaxLength(1000)
                          .IsRequired();
            shippingAddressProp.Property(n => n.NormalizedValue)
                          .HasColumnName($"Normalized{nameof(Order.ShippingAddress)}")
                          .HasMaxLength(1000)
                          .IsRequired();
        });
        builder.Property(o => o.CompletedOnUtc).IsRequired(false);

        builder.Property(o => o.OrderSubTotal);
        builder.Property(o => o.OrderTotal);

        builder.Property(o => o.CreatedByUserId);
        builder.Property(o => o.CreatedOnUtc);
        builder.Property(o => o.ExpectedShippingDateUtc);
        builder.Property(o => o.OrderDiscount);

        builder.Property(o => o.CreatedByUsername).HasMaxLength(1000);
        builder.Property(o => o.Note);

        builder.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId);

        builder.ComplexProperty(o => o.CustomerInfo, customerInfoBuilder =>
        {
            customerInfoBuilder.Property(info => info.FullName)
                .HasColumnName($"CustomerName")
                .HasMaxLength(200)
                .HasConversion(
                    info => info.Value,
                    value => new NormalizableString(value)
                );
            customerInfoBuilder.Property(info => info.Address)
                .HasColumnName($"Customer{nameof(CustomerInfo.Address)}")
                .HasMaxLength(500)
                .HasConversion(
                    info => info.Value,
                    value => new NormalizableString(value)
                );
            customerInfoBuilder.Property(c => c.PhoneNumber)
                .HasColumnName("CustomerPhone")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.HasMany(o => o.OrderItems).WithOne().HasForeignKey(oi => oi.OrderId);
        builder.Navigation(o => o.OrderItems).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
