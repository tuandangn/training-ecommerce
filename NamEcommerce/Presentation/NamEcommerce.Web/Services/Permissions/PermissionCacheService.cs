using Microsoft.Extensions.Caching.Memory;
using NamEcommerce.Domain.Entities.Security;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Web.Services.Permissions;

public sealed class PermissionCacheService(
    IEntityDataReader<Permission> permissionReader,
    IEntityDataReader<RolePermission> rolePermissionReader,
    IEntityDataReader<Role> roleReader,
    IMemoryCache cache) : IPermissionCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static string CacheKey(string roleName) => $"permissions:role:{roleName.ToUpperInvariant()}";

    public async Task<IReadOnlySet<string>> GetPermissionsForRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var key = CacheKey(roleName);
        if (cache.TryGetValue(key, out HashSet<string>? cached) && cached is not null)
            return cached;

        var role = roleReader.DataSource
            .FirstOrDefault(r => r.NormalizedName == SystemUserRoleNames.Normalize(roleName));

        if (role is null)
            return new HashSet<string>();

        var permissionIds = rolePermissionReader.DataSource
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var permissions = permissionReader.DataSource
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.NormalizedName)
            .ToHashSet();

        cache.Set(key, permissions, CacheDuration);
        return permissions;
    }

    public void Invalidate(string roleName)
        => cache.Remove(CacheKey(roleName));

    public void InvalidateAll()
    {
        foreach (var roleName in SystemUserRoleNames.All)
            Invalidate(roleName);
    }
}
