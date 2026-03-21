using MediatR;
using NamEcommerce.Application.Contracts.Commands.Models;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Queries.Users;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Commands.Handlers;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResultDto>
{
    private readonly IMediator _mediator;
    private readonly IUserManager _userManager;
    private readonly ISecurityService _securityService;

    public CreateUserHandler(IMediator mediator, IUserManager userManager, ISecurityService securityService)
    {
        _mediator = mediator;
        _userManager = userManager;
        _securityService = securityService;
    }

    public async Task<CreateUserResultDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _mediator.Send(new DoesUsernameExist(request.Dto.Username), cancellationToken))
        {
            return new CreateUserResultDto
            {
                Success = false,
                ErrorMessage = "Username already exists."
            };
        }
        var userInfo = request.Dto;
        var (passwordHash, passwordSalt) = await _securityService.HashPasswordAsync(userInfo.Password);
        var user = await _userManager.CreateUserAsync(
            new NamEcommerce.Domain.Shared.Dtos.Users.CreateUserDto
            {
                Username = userInfo.Username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                FullName = userInfo.FullName,
                PhoneNumber = userInfo.PhoneNumber,
                Address = userInfo.Address
            });

        return new CreateUserResultDto
        {
            Success = user != null,
            CreatedId = user?.CreatedId,
            ErrorMessage = user == null ? "Failed to create user." : null
        };
    }
}
