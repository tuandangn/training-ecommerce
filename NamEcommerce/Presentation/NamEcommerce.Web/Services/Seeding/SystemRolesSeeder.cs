using NamEcommerce.Application.Contracts.Users;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class SystemRolesSeeder(IUserAppService userAppService) : IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
        => userAppService.EnsureSystemRolesAsync();
}
