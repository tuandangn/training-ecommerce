using MediatR;
using NamEcommerce.Application.Contracts.Commands.Models;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Queries.Users;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Commands.Handlers;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResultAppDto>
{
    private readonly IMediator _mediator;
    private readonly IUserManager _userManager;

    public CreateUserHandler(IMediator mediator, IUserManager userManager)
    {
        _mediator = mediator;
        _userManager = userManager;
    }

    public async Task<CreateUserResultAppDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _mediator.Send(new DoesUsernameExist(request.Dto.Username), cancellationToken))
        {
            return new CreateUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Username already exists."
            };
        }
        var userInfo = request.Dto;
        var user = await _userManager.CreateUserAsync(
            new CreateUserDto
            {
                Username = userInfo.Username,
                Password = userInfo.Password,
                FullName = userInfo.FullName,
                PhoneNumber = userInfo.PhoneNumber,
                Address = userInfo.Address
            });

        return new CreateUserResultAppDto
        {
            Success = user != null,
            CreatedId = user?.CreatedId,
            ErrorMessage = user == null ? "Failed to create user." : null
        };
    }
}
