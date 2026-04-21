using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Contracts.Services;

public interface ICurrentUserService
{
    ValueTask<bool> IsAuthenticatedAsync();

    ValueTask<CurrentUserInfoModel?> GetCurrentUserInfoAsync();
}
