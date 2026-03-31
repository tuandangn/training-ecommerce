using MediatR;
using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Contracts.Commands.Models.Users;

[Serializable]
public record class RegisterUserCommand : IRequest<RegisterUserResult>
{
    public RegisterUserCommand(string username, string password, string fullname)
        => (Username, Password, FullName) = (username, password, fullname);

    public string Username { get; init; }
    public string Password { get; init; }
    public string FullName { get; init; }
    public string? Address { get; set; }
    public required string PhoneNumber { get; init; }
}

