using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class AdminUserSeeder(
    IUserAppService userAppService,
    IConfiguration configuration) : IDataSeeder
{
    private const string DefaultUsername = "Administrator";
    private const string DefaultPassword = "12345678";
    private const string DefaultFullName = "Administrator";
    private const string DefaultPhone = "0900000000";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var hasAdmin = await userAppService.HasUsersInRoleAsync(SystemUserRoleNames.Admin).ConfigureAwait(false);
        if (hasAdmin)
            return;

        await userAppService.EnsureSystemRolesAsync().ConfigureAwait(false);

        var username = configuration["SeedData:Admin:Username"] ?? DefaultUsername;
        var password = configuration["SeedData:Admin:Password"] ?? DefaultPassword;

        var createResult = await userAppService.CreateUserAsync(new CreateUserAppDto(
            username: username,
            password: password,
            fullName: DefaultFullName,
            phoneNumber: DefaultPhone
        )).ConfigureAwait(false);

        if (!createResult.Success || !createResult.CreatedId.HasValue)
            return;

        var roles = await userAppService.GetRolesAsync().ConfigureAwait(false);
        var adminRoleId = roles.FirstOrDefault(r =>
            string.Equals(r.Name, SystemUserRoleNames.Admin, StringComparison.OrdinalIgnoreCase))?.Id;

        if (!adminRoleId.HasValue)
            return;

        await userAppService.UpdateUserRolesAsync(new UpdateUserRolesAppDto
        {
            UserId = createResult.CreatedId.Value,
            RoleIds = [adminRoleId.Value]
        }).ConfigureAwait(false);
    }
}
