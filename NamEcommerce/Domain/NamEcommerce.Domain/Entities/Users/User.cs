using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.Users;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Security;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record User : AppAggregateEntity
{
    #region Ctors

    internal User(string username, string fullName, string phoneNumber)
        : this(Guid.NewGuid(), username, fullName, phoneNumber)
    {
    }

    internal User(Guid id, string username, string fullName, string phoneNumber)
        : this(id, username, fullName, phoneNumber, string.Empty, string.Empty, null)
    {
    }

    private User(Guid id, string username, string fullName, string phoneNumber, string passwordHash, string passwordSalt, string? address)
        : base(id)
    {
        (Username, FullName, PhoneNumber) = (username, fullName, phoneNumber);
        (PasswordHash, PasswordSalt, Address) = (passwordHash, passwordSalt, address);

        CreatedOnUtc = DateTime.UtcNow;
    }

    #endregion

    #region Properties

    public string Username { get; init; }

    public string PasswordHash { get; private set; }
    public string PasswordSalt { get; private set; }

    public string FullName
    {
        get;
        set
        {
            field = value;
            NormalizedFullName = TextHelper.Normalize(value);
        }
    }
    public string NormalizedFullName { get; internal set; } = "";

    public string? Address
    {
        get;
        set
        {
            field = value;
            NormalizedAddress = TextHelper.Normalize(value);
        }
    }
    public string NormalizedAddress { get; internal set; } = "";

    public string PhoneNumber { get; private set; }

    public DateTime CreatedOnUtc { get; }

    #endregion

    #region Methods

    internal async Task SetPasswordAsync(string password, ISecurityService securityService)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(securityService);

        var (passwordHash, passwordSalt) = await securityService.HashPasswordAsync(password).ConfigureAwait(false);
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
    }

    static internal async Task<User> CreateAsync(
        string username, string fullName, string phoneNumber, string password, string? address,
        IUsernameExistCheckingService usernameChecker, ISecurityService securityService)
    {
        if (string.IsNullOrEmpty(username))
            throw new UserDataIsInvalidException("Tên tài khoản không được để trống");
        if (string.IsNullOrEmpty(fullName))
            throw new UserDataIsInvalidException("Họ tên không được để trống");
        if (string.IsNullOrEmpty(phoneNumber))
            throw new UserDataIsInvalidException("Số điện thoại không được để trống");
        if (string.IsNullOrEmpty(password))
            throw new UserDataIsInvalidException("Mật khẩu không được để trống");

        if (await usernameChecker.DoesUsernameExistAsync(username).ConfigureAwait(false))
            throw new UsernameExistsException(username);

        ArgumentNullException.ThrowIfNull(securityService);
        var (passwordHash, passwordSalt) = await securityService.HashPasswordAsync(password).ConfigureAwait(false);

        var user = new User(default, username, fullName, phoneNumber, passwordHash, passwordSalt, address);

        return user;
    }

    #endregion

    #region Domain Event Markers

    /// <summary>
    /// Đánh dấu user vừa được tạo — Manager gọi trước <c>InsertAsync</c>.
    /// Event sẽ được dispatch sau khi <c>SaveChanges</c> thành công bởi <c>DomainEventDispatchInterceptor</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new UserCreated(Id, Username, FullName));

    /// <summary>
    /// Đánh dấu thông tin user vừa cập nhật — raise <see cref="UserUpdated"/>.
    /// </summary>
    internal void MarkUpdated()
        => RaiseDomainEvent(new UserUpdated(Id));

    /// <summary>
    /// Đánh dấu mật khẩu user vừa được đổi — raise <see cref="UserPasswordChanged"/>.
    /// Manager gọi sau khi <see cref="SetPasswordAsync"/> + <c>UpdateAsync</c> thành công.
    /// </summary>
    internal void MarkPasswordChanged()
        => RaiseDomainEvent(new UserPasswordChanged(Id));

    /// <summary>
    /// Đánh dấu user bị xoá — raise <see cref="UserDeleted"/>.
    /// </summary>
    internal void MarkDeleted()
        => RaiseDomainEvent(new UserDeleted(Id, Username));

    #endregion

    #region Factory



    #endregion
}
