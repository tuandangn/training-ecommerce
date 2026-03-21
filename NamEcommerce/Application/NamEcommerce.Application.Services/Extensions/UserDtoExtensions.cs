using NamEcommerce.Application.Contracts.Dtos.Users;

namespace NamEcommerce.Application.Services.Extensions;

public static class UserDtoExtensions
{
    public static UserAppDto ToDto(this Domain.Shared.Dtos.Users.UserDto user)
        => new UserAppDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CreatedOnUtc = user.CreatedOnUtc
        };
}
