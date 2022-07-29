using NamEcommerce.Domain.Shared.Exceptions.Users;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record User : AppEntity
{
    internal User(int id, string username, string passwordHash, string fullName, string phoneNumber)
        : this(id, username, passwordHash, fullName, phoneNumber, Array.Empty<UserRole>())
    { }

    internal User(int id, string username, string passwordHash, string fullName, string phoneNumber, IList<UserRole> userRoles)
        : base(id)
        => (Username, PasswordHash, FullName, PhoneNumber, _userRoles)
            = (username, passwordHash, fullName, phoneNumber, userRoles);

    public string Username { get; init; }
    public string PasswordHash { get; init; }

    public string FullName { get; init; }
    public string? Address { get; set; }
    public string PhoneNumber { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    private IList<UserRole> _userRoles;
    public IEnumerable<UserRole> UserRoles => _userRoles.AsEnumerable();

    #region Methods

    internal void SetUserRoles(IList<UserRole> userRoles)
    {
        if (userRoles is null)
            throw new ArgumentNullException(nameof(userRoles));

        _userRoles = userRoles;
    }
    internal void AddToRole(int roleId)
    {
        if (UserRoles.Any(ur => ur.RoleId == roleId))
            throw new UserAlreadyHaveRoleException(roleId, Username);

        _userRoles.Add(new UserRole(default, Id, roleId));
    }

    #endregion
}
