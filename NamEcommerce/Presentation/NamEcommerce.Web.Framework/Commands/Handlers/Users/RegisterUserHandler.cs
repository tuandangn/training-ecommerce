using MediatR;
using Microsoft.AspNetCore.Http;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Users;
using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Users;

public sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAppService _userAppService;
    private readonly IMediator _mediator;

    public RegisterUserHandler(IHttpContextAccessor httpContextAccessor, IUserAppService userAppService, IMediator mediator)
    {
        _httpContextAccessor = httpContextAccessor;
        _userAppService = userAppService;
        _mediator = mediator;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await _userAppService.DoesUsernameExistsAsync(request.Username);
        if (exists)
        {
            return new RegisterUserResult
            {
                Success = false,
                ErrorMessage = "Username already exists."
            };
        }

        var createUserResult = await _userAppService.CreateUserAsync(new CreateUserAppDto(
            request.Username, request.Password, request.FullName,
            request.PhoneNumber)
        {
            Address = request.Address
        });
        if (!createUserResult.Success)
        {
            return new RegisterUserResult
            {
                Success = false,
                ErrorMessage = createUserResult.ErrorMessage
            };
        }

        var authenticateResult = await _mediator.Send(new AuthenticateUserCommand(request.Username, request.Password), cancellationToken);
        if (!authenticateResult.Success)
        {
            return new RegisterUserResult
            {
                Success = false,
                CreatedId = createUserResult.CreatedId,
                ErrorMessage = authenticateResult.ErrorMessage
            };
        }

        return new RegisterUserResult
        {
            Success = true,
            CreatedId = createUserResult.CreatedId
        };
    }
}
