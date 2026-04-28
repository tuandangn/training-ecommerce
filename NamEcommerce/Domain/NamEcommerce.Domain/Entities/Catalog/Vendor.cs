using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.Catalog;
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

    /// <summary>
    /// Đánh dấu vendor vừa được khởi tạo — Manager gọi trước <c>InsertAsync</c>.
    /// Event sẽ được dispatch sau khi <c>SaveChanges</c> thành công bởi <c>DomainEventDispatchInterceptor</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new VendorCreated(Id, Name, PhoneNumber));

    /// <summary>
    /// Đánh dấu vendor vừa được cập nhật — raise <see cref="VendorUpdated"/>.
    /// </summary>
    internal void MarkUpdated()
        => RaiseDomainEvent(new VendorUpdated(Id));

    /// <summary>
    /// Đánh dấu vendor bị xoá — Manager gọi trước <c>DeleteAsync</c> để raise <see cref="VendorDeleted"/>.
    /// </summary>
    internal void MarkDeleted()
        => RaiseDomainEvent(new VendorDeleted(Id, Name));

    #endregion
}
