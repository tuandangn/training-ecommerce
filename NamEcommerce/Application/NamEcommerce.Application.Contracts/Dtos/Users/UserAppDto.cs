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
