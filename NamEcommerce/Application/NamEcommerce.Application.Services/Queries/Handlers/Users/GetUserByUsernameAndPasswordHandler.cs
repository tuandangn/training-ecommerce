using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Queries.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Queries.Handlers.Users;

public sealed class GetUserByUsernameAndPasswordHandler : IRequestHandler<GetUserByUsernameAndPassword, UserAppDto?>
{
    private readonly IUserManager _userManager;

    public GetUserByUsernameAndPasswordHandler(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserAppDto?> Handle(GetUserByUsernameAndPassword request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Username))
            throw new ArgumentException("Username is required", nameof(request.Username));
        if (string.IsNullOrEmpty(request.Password))
            throw new ArgumentException("Password is required", nameof(request.Password));

        var user = await _userManager.FindUserByUserNameAndPasswordAsync(request.Username, request.Password);

        return user?.ToDto();
    }
}
