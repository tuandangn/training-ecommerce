using NamEcommerce.Domain.Entities.Customers;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class WarehouseMapping : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(nameof(Warehouse), DbScheme);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.ComplexProperty(c => c.Name, fullNameProp =>
        {
            fullNameProp.Property(n => n.Value)
                          .HasColumnName(nameof(Warehouse.Name))
                          .HasMaxLength(200)
                          .IsRequired();
            fullNameProp.Property(n => n.NormalizedValue)
                          .HasColumnName($"Normalized{nameof(Warehouse.Name)}")
                          .HasMaxLength(200)
                          .IsRequired();
        });
        builder.ComplexProperty(c => c.Address, fullNameProp =>
        {
            fullNameProp.Property(n => n.Value)
                          .HasColumnName(nameof(Warehouse.Address))
                          .HasMaxLength(800);
            fullNameProp.Property(n => n.NormalizedValue)
                          .HasColumnName($"Normalized{nameof(Warehouse.Address)}")
                          .HasMaxLength(800);
        });
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.WarehouseType).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.ManagerUserId);
    }
}
