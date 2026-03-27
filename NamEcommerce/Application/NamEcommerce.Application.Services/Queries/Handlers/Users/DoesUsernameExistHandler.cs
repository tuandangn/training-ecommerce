using MediatR;
using NamEcommerce.Application.Contracts.Queries.Users;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Queries.Handlers.Users;

public sealed class DoesUsernameExistHandler : IRequestHandler<DoesUsernameExist, bool>
{
    private readonly IUserManager _userManager;

    public DoesUsernameExistHandler(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> Handle(DoesUsernameExist request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Username))
            throw new ArgumentException("Username cannot be null or empty.", nameof(request.Username));

        var exists = await _userManager.DoesUsernameExistAsync(request.Username);

        return exists;
    }
}
