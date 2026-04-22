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
