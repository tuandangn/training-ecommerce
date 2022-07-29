namespace NamEcommerce.Data.SqlServer.Mappings;

public sealed class RolePermissionMapping : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable(nameof(RolePermission), DbScheme);

        builder.HasKey(x => x.Id);

        builder.HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId);
        builder.HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId);
    }
}
