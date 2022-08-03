namespace NamEcommerce.Application.Shared.Dtos.Users;

[Serializable]
public sealed record UserDto
{
    public UserDto(int id, string username, string fullName, string phoneNumber)
        => (Id, Username, FullName, PhoneNumber) = (id, username, fullName, phoneNumber);

    public int Id { get; }

    public string Username { get; }

    public string FullName { get; }

    public string PhoneNumber { get; }
}
