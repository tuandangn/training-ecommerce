namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(nameof(User), DbScheme);

        builder.HasKey(x => x.Id);

        builder.Property(u => u.Username).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Address).HasMaxLength(400);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
    }
}
