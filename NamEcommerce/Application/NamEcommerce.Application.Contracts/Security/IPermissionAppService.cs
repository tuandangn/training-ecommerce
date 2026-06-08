using NamEcommerce.Application.Contracts.Dtos.Security;

namespace NamEcommerce.Application.Contracts.Security;

public interface IPermissionAppService
{
    Task<RolePermissionsAppDto?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage)> UpdateRolePermissionsAsync(Guid roleId, IList<Guid> permissionIds, CancellationToken cancellationToken = default);
}
