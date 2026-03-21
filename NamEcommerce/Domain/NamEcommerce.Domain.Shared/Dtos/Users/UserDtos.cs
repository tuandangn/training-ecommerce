namespace NamEcommerce.Domain.Shared.Dtos.Users;

[Serializable]
public abstract record BaseUserDto
{
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Address { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Username))
            return (false, "Username cannot be null or empty.");

        if (string.IsNullOrEmpty(FullName))
            return (false, "Fullname cannot be null or empty.");

        if (string.IsNullOrEmpty(PhoneNumber))
            return (false, "Phone number cannot be null or empty.");

        return (true, null);
    }
}

[Serializable]
public sealed record UserDto(Guid Id) : BaseUserDto
{
    public DateTime CreatedOnUtc { get; set; }
}

[Serializable]
public sealed record CreateUserDto : BaseUserDto
{
    public required string PasswordHash { get; init; }
    public required string PasswordSalt { get; init; }

    public (bool valid, string? errorMessage) Validate(CreateUserDto dto)
    {
        if (dto is null)
            return (false, "CreateUserDto cannot be null.");

        var baseValidateResult = base.Validate();
        if (!baseValidateResult.valid)
            return baseValidateResult;

        if (string.IsNullOrEmpty(dto.PasswordHash))
            return (false, "Password hash cannot be null or empty.");

        if (string.IsNullOrEmpty(dto.PasswordSalt))
            return (false, "Password salt cannot be null or empty.");

        return (true, null);
    }
}
[Serializable]
public sealed record CreateUserResultDto
{
    public required Guid CreatedId { get; init; }
}
