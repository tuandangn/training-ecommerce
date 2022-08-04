namespace NamEcommerce.Application.Shared.Dtos.Users;

[Serializable]
public sealed record RegisterDto
{
    public RegisterDto(string username, string password, string fullName, string phoneNumber)
        => (Username, Password, FullName, PhoneNumber) = (username, password, fullName, phoneNumber);

    public string Username { get; init; }

    public string Password { get; init; }

    public string FullName { get; init; }

    public string PhoneNumber { get; init; }

    public string? Address { get; set; }
}
