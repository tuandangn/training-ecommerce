namespace NamEcommerce.Web.Services.Permissions;

public interface IPermissionCacheService
{
    Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(string roleName, CancellationToken cancellationToken = default);
    void Invalidate(string roleName);
    void InvalidateAll();
}
