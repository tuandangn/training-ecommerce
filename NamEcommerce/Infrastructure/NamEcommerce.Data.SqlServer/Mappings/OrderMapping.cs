namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class OrderMapping : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(nameof(Order), DbScheme);

        builder.HasKey(o => o.Id);

        builder.HasOne<User>().WithMany().HasForeignKey(o => o.UserId);
        builder.HasMany(o => o.OrderItems).WithOne().HasForeignKey(oi => oi.OrderId);
    }
}
