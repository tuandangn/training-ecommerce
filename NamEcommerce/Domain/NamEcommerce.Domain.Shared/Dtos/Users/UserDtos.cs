using NamEcommerce.Domain.Shared.Exceptions.Catalog;

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
            throw new UserDataIsInvalidException("Username cannot be null or empty.");
        if (string.IsNullOrEmpty(FullName))
            throw new UserDataIsInvalidException("Fullname cannot be null or empty.");
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new UserDataIsInvalidException("Phone number cannot be null or empty.");
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
            throw new UserDataIsInvalidException("Password cannot be null or empty.");
    }
}
[Serializable]
public sealed record CreateUserResultDto
{
    public required Guid CreatedId { get; init; }
}
