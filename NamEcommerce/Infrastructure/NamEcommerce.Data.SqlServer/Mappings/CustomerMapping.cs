using NamEcommerce.Domain.Entities.Customers;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerMapping : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(nameof(Customer), DbScheme);
        builder.HasKey(c => c.Id);

        builder.ComplexProperty(c => c.FullName, fullNameProp =>
        {
            fullNameProp.Property(n => n.Value)
                          .HasColumnName(nameof(Customer.FullName))
                          .HasMaxLength(200)
                          .IsRequired();
            fullNameProp.Property(n => n.NormalizedValue)
                          .HasColumnName($"Normalized{nameof(Customer.FullName)}")
                          .HasMaxLength(200)
                          .IsRequired();
        });
        builder.ComplexProperty(c => c.Address, addressProp =>
        {
            addressProp.Property(n => n.Value)
                          .HasColumnName(nameof(Customer.Address))
                          .HasMaxLength(500)
                          .IsRequired();
            addressProp.Property(n => n.NormalizedValue)
                          .HasColumnName($"Normalized{nameof(Customer.Address)}")
                          .HasMaxLength(500)
                          .IsRequired();
        });
        builder.Property(c => c.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Note).HasMaxLength(1000);
        builder.Property(c => c.Kind).HasColumnName("CustomerKind").HasConversion<int>().IsRequired();
        builder.Property(c => c.IsSystem).IsRequired();
        builder.HasIndex(c => new { c.Kind, c.IsSystem })
            .IsUnique()
            .HasDatabaseName("IX_Customer_CustomerKind_IsSystem")
            .HasFilter("[CustomerKind] = 20 AND [IsSystem] = 1 AND [IsDeleted] = 0");
    }
}
