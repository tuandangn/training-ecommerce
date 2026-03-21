using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Domain.Services.Extensions;

public static class UserExtensions
{
    public static UserDto ToDto(this User user)
        => new UserDto(user.Id)
        {
            Username = user.Username,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CreatedOnUtc = user.CreatedOnUtc
        };
}
