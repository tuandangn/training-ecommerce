using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Security;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record User : AppAggregateEntity
{
    internal User(string username, string fullName, string phoneNumber)
        : this(Guid.NewGuid(), username, fullName, phoneNumber)
    {
    }

    internal User(Guid id, string username, string fullName, string phoneNumber)
        : base(id)
    {
        (Username, FullName, PhoneNumber)
                = (username, fullName, phoneNumber);

        NormalizedFullName = TextHelper.Normalize(FullName);
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Username { get; init; }

    public string? PasswordHash { get; private set; }
    public string? PasswordSalt { get; private set; }

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

    #region Methods

    internal async Task SetPasswordAsync(string password, ISecurityService securityService)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(securityService);

        var (passwordHash, passwordSalt) = await securityService.HashPasswordAsync(password).ConfigureAwait(false);
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
    }

    #endregion
}
