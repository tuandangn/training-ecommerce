using NamEcommerce.Application.Contracts.Dtos.Security;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Security;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Application.Services.Security;

public sealed class PermissionAppService(
    IEntityDataReader<Permission> permissionReader,
    IEntityDataReader<Role> roleReader,
    IEntityDataReader<RolePermission> rolePermissionReader,
    IRepository<RolePermission> rolePermissionRepository) : IPermissionAppService
{
    public Task<RolePermissionsAppDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = roleReader.DataSource.FirstOrDefault(r => r.Id == roleId);
        if (role is null)
            return Task.FromResult<RolePermissionsAppDto?>(null);

        var allPermissions = permissionReader.DataSource
            .Select(p => new { p.Id, p.Name })
            .ToList();

        var grantedIds = rolePermissionReader.DataSource
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        var dto = new RolePermissionsAppDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            AllPermissions = allPermissions
                .Select(p => new PermissionAppDto { Id = p.Id, Name = p.Name })
                .ToList(),
            GrantedPermissionIds = grantedIds
        };

        return Task.FromResult<RolePermissionsAppDto?>(dto);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateRolePermissionsAsync(
        Guid roleId, IList<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var current = rolePermissionReader.DataSource
            .Where(rp => rp.RoleId == roleId)
            .ToList();

        var desired = permissionIds.ToHashSet();
        var existing = current.ToDictionary(rp => rp.PermissionId);

        foreach (var rp in current.Where(rp => !desired.Contains(rp.PermissionId)))
            await rolePermissionRepository.DeleteAsync(rp, cancellationToken).ConfigureAwait(false);

        foreach (var permissionId in desired.Where(id => !existing.ContainsKey(id)))
            await rolePermissionRepository.InsertAsync(new RolePermission(roleId, permissionId), cancellationToken).ConfigureAwait(false);

        return (true, null);
    }
}
