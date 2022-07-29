namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class UserRoleMapping : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable(nameof(UserRole), DbScheme);

        builder.HasKey(ur => ur.Id);

        builder.HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId);
        builder.HasOne<User>().WithMany(u => u.UserRoles).HasForeignKey(rp => rp.UserId);
    }
}
