namespace NamEcommerce.Application.Contracts.Dtos.Users;

[Serializable]
public sealed record UserAppDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }

    public string? Address { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public IList<string> RoleNames { get; init; } = [];
}

[Serializable]
public sealed record CreateUserAppDto
{
    public CreateUserAppDto(string username, string password, string fullName, string phoneNumber)
        => (Username, Password, FullName, PhoneNumber) = (username, password, fullName, phoneNumber);

    public string Username { get; init; }
    public string Password { get; init; }
    public string FullName { get; init; }
    public string PhoneNumber { get; init; }

    public string? Address { get; set; }

    public static (bool valid, string? errorMessage) Validate(CreateUserAppDto dto)
    {
        if (dto is null)
            return (false, "Error.UserDtoRequired");

        if (string.IsNullOrEmpty(dto.Username))
            return (false, "Error.UsernameRequired");
        if (string.IsNullOrEmpty(dto.Password))
            return (false, "Error.PasswordRequired");
        if (string.IsNullOrEmpty(dto.FullName))
            return (false, "Error.FullNameRequired");
        if (string.IsNullOrEmpty(dto.PhoneNumber))
            return (false, "Error.PhoneNumberRequired");

        return (true, null);
    }
}
[Serializable]
public sealed record CreateUserResultAppDto
{
    public bool Success { get; set; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record RoleAppDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

[Serializable]
public sealed record UpdateUserRolesAppDto
{
    public required Guid UserId { get; init; }
    public IList<Guid> RoleIds { get; init; } = [];

    public static (bool valid, string? errorMessage) Validate(UpdateUserRolesAppDto dto)
    {
        if (dto is null)
            return (false, "Error.UserRoleDtoRequired");

        if (dto.UserId == Guid.Empty)
            return (false, "Error.UserRequired");

        if (dto.RoleIds is null)
            return (false, "Error.RoleIdsRequired");

        if (dto.RoleIds.Any(roleId => roleId == Guid.Empty))
            return (false, "Error.RoleIsInvalid");

        if (dto.RoleIds.Distinct().Count() != dto.RoleIds.Count)
            return (false, "Error.RoleDuplicated");

        return (true, null);
    }
}

[Serializable]
public sealed record UpdateUserRolesResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
