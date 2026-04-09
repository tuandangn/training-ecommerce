using NamEcommerce.Domain.Entities.Customers;

namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class CustomerMapping : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(nameof(Customer), DbScheme);
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Address).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Note).HasMaxLength(1000);

        builder.Property(u => u.NormalizedFullName).HasMaxLength(400);
        builder.Property(u => u.NormalizedAddress).HasMaxLength(1000);
    }
}
