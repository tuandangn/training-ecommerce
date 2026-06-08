using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Customer.Api.Infrastructure;

internal sealed class CustomerApiCurrentUserAccessor : ICurrentUserAccessor
{
    public Task<CurrentUserInfoDto?> GetCurrentUserAsync()
        => Task.FromResult<CurrentUserInfoDto?>(null);
}
