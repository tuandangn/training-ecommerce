using MediatR;
using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Contracts.Commands.Models;

[Serializable]
public sealed record AuthenticateUserCommand : IRequest<AuthenticateUserResult>
{
    public AuthenticateUserCommand(string username, string password)
        => (Username, Password) = (username, password);

    public string Username { get; init; }
    public string Password { get; init; }
}
