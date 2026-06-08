using NamEcommerce.Domain.Shared.Exceptions.Users;

namespace NamEcommerce.Domain.Shared.Dtos.Users;

[Serializable]
public abstract record BaseUserDto
{
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Address { get; set; }

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Username))
            throw new UserDataIsInvalidException("Error.UsernameRequired");
        if (string.IsNullOrEmpty(FullName))
            throw new UserDataIsInvalidException("Error.FullNameRequired");
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new UserDataIsInvalidException("Error.PhoneNumberRequired");
    }
}

[Serializable]
public sealed record UserDto(Guid Id) : BaseUserDto
{
    public DateTime CreatedOnUtc { get; set; }
    public IList<string> RoleNames { get; init; } = [];
}

[Serializable]
public sealed record CreateUserDto : BaseUserDto
{
    public required string Password { get; init; }

    public override void Verify()
    {
        base.Verify();

        if (string.IsNullOrEmpty(Password))
            throw new UserDataIsInvalidException("Error.PasswordRequired");
    }
}
[Serializable]
public sealed record CreateUserResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record RoleDto(Guid Id)
{
    public required string Name { get; init; }
    public required string NormalizedName { get; init; }
}

[Serializable]
public sealed record UpdateUserRolesDto(Guid UserId, IList<Guid> RoleIds)
{
    public void Verify()
    {
        if (UserId == Guid.Empty)
            throw new UserDataIsInvalidException("Error.UserRequired");

        if (RoleIds is null)
            throw new UserDataIsInvalidException("Error.RoleIdsRequired");

        if (RoleIds.Any(roleId => roleId == Guid.Empty))
            throw new UserDataIsInvalidException("Error.RoleIsInvalid");

        if (RoleIds.Distinct().Count() != RoleIds.Count)
            throw new UserDataIsInvalidException("Error.RoleDuplicated");
    }
}

public static class SystemUserRoleNames
{
    public const string Admin = "Admin";
    public const string SalesStaff = "SalesStaff";
    public const string WarehouseManager = "WarehouseManager";
    public const string DeliveryStaff = "DeliveryStaff";
    public const string Cashier = "Cashier";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        SalesStaff,
        WarehouseManager,
        DeliveryStaff,
        Cashier
    ];

    public static string Normalize(string roleName)
        => roleName.Trim().ToUpperInvariant();
}
