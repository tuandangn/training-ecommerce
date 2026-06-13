using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Domain.Services.Extensions;

public static class UserExtensions
{
    public static UserDto ToDto(this User user, IList<string>? roleNames = null)
        => new UserDto(user.Id)
        {
            Username = user.Username,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            CreatedOnUtc = user.CreatedOnUtc,
            IsLocked = user.IsLocked,
            RoleNames = roleNames ?? []
        };

    public static RoleDto ToDto(this Role role)
        => new RoleDto(role.Id)
        {
            Name = role.Name,
            NormalizedName = role.NormalizedName
        };
}
