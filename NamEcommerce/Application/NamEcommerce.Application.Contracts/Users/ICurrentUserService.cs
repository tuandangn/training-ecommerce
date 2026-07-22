using NamEcommerce.Application.Contracts.Dtos.Users;

namespace NamEcommerce.Application.Contracts.Users;

public interface ICurrentUserService
{
    ValueTask<CurrentUserInfoAppDto?> GetCurrentUserInfoAsync();

    ValueTask<bool> IsAuthenticatedAsync();
    Task<bool> IsAdminAsync();
    Task<bool> IsWarehouseManager();
    Task<bool> IsInRole(string roleName);
}
