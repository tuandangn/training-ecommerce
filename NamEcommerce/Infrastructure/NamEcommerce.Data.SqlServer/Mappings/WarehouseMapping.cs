namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class WarehouseMapping : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(nameof(Warehouse), DbScheme);

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.NormalizedName).HasMaxLength(400);
        builder.Property(p => p.Address).HasMaxLength(800);
        builder.Property(p => p.NormalizedAddress).HasMaxLength(1600);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
    }
}
