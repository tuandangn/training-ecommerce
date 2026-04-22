using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Vendor : AppAggregateEntity
{
    internal Vendor(Guid id, string name, string phoneNumber) : base(id)
    {
        if (string.IsNullOrEmpty(name))
            throw new VendorNameRequiredException();
        if (string.IsNullOrEmpty(phoneNumber))
            throw new VendorPhoneNumberRequiredException();

        (Name, PhoneNumber) = (name, phoneNumber);
        NormalizedName = TextHelper.Normalize(Name);
        NormalizedAddress = TextHelper.Normalize(Address);
    }

    public string Name { get; private set; }
    internal string NormalizedName { get; private set; } = "";
    public string PhoneNumber { get; internal set; }
    public string? Address
    {
        get;
        internal set
        {
            field = value;
            NormalizedAddress = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedAddress { get; private set; } = "";

    public int DisplayOrder { get; set; }

    #region Methods

    internal async Task SetNameAsync(string name, INameExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(name))
            throw new VendorNameRequiredException();

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new VendorNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    #endregion
}
