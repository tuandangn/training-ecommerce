using NamEcommerce.Web.Contracts.Dtos;

namespace NamEcommerce.Web.Contracts.Services;

public interface ICurrentUserService
{
    ValueTask<bool> IsAuthenticatedAsync();

    ValueTask<CurrentUserInfoDto?> GetCurrentUserInfoAsync();
}
