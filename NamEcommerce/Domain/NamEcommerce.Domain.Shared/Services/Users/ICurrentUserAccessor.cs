using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Domain.Shared.Services.Users;

public interface ICurrentUserAccessor
{
    Task<CurrentUserInfoDto?> GetCurrentUserAsync();
}
