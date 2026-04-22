using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Users;

public sealed class UserAppService : IUserAppService
{
    private readonly IUserManager _userManager;

    public UserAppService(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task<CreateUserResultAppDto> CreateUserAsync(CreateUserAppDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var (valid, errorMessage) = CreateUserAppDto.Validate(dto);
        if (!valid)
        {
            return new CreateUserResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _userManager.DoesUsernameExistAsync(dto.Username).ConfigureAwait(false))
        {
            return new CreateUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.UsernameAlreadyExists"
            };
        }
        var user = await _userManager.CreateUserAsync(
            new CreateUserDto
            {
                Username = dto.Username,
                Password = dto.Password,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address
            });

        return new CreateUserResultAppDto
        {
            Success = user != null,
            CreatedId = user?.CreatedId,
            ErrorMessage = user == null ? "Error.UserCreateFailed" : null
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