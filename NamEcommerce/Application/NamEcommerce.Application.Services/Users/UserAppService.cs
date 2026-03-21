using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Queries.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Application.Services.Users;

public sealed class UserAppService : IUserAppService
{
    private readonly IUserManager _userManager;
    private readonly ISecurityService _securityService;

    public UserAppService(IUserManager userManager, ISecurityService securityService)
    {
        _userManager = userManager;
        _securityService = securityService;
    }

    public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var (valid, errorMessage) = CreateUserDto.Validate(dto);
        if (!valid)
        {
            return new CreateUserResultDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _userManager.DoesUsernameExistAsync(dto.Username).ConfigureAwait(false))
        {
            return new CreateUserResultDto
            {
                Success = false,
                ErrorMessage = "Username already exists."
            };
        }
        var (passwordHash, passwordSalt) = await _securityService.HashPasswordAsync(dto.Password);
        var user = await _userManager.CreateUserAsync(
            new NamEcommerce.Domain.Shared.Dtos.Users.CreateUserDto
            {
                Username = dto.Username,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address
            });

        return new CreateUserResultDto
        {
            Success = user != null,
            CreatedId = user?.CreatedId,
            ErrorMessage = user == null ? "Failed to create user." : null
        };
    }

    public async Task<bool> DoesUsernameExistsAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException(nameof(username));

        return await _userManager.DoesUsernameExistAsync(username);
    }

    public async Task<UserAppDto?> GetUserByUsernameAndPasswordAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException(nameof(username));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(password);

        var user = await _userManager.FindUserByUserNameAndPasswordAsync(username, password);

        return user?.ToDto();
    }
}
