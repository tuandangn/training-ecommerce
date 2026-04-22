using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record Warehouse : AppAggregateEntity
{
    internal Warehouse(string code, string name, WarehouseType warehouseType) : base(Guid.NewGuid())
    {
        if (string.IsNullOrEmpty(code))
            throw new WarehouseCodeRequiredException();
        if (string.IsNullOrEmpty(name))
            throw new WarehouseNameRequiredException();

        Code = code;
        Name = name;
        WarehouseType = warehouseType;

        NormalizedName = TextHelper.Normalize(Name);
        NormalizedAddress = TextHelper.Normalize(Address);
    }

    public string Code { get; private set; }
    public string Name { get; private set; }
    internal string NormalizedName { get; private set; } = "";

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

    public string? PhoneNumber { get; internal set; }
    public Guid? ManagerUserId { get; internal set; }

    public WarehouseType WarehouseType { get; private set; }
    public bool IsActive { get; private set; }

    #region Methods

    internal async Task SetNameAsync(string name, INameExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(name))
            throw new WarehouseNameRequiredException();

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new WarehouseNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    internal async Task SetCodeAsync(string code, ICodeExistCheckingService checker)
    {
        if (string.Equals(Name, code, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(code))
            throw new WarehouseCodeRequiredException();

        if (await checker.DoesCodeExistAsync(code, Id).ConfigureAwait(false))
            throw new WarehouseCodeExistsException(code);

        Code = code;
    }

    internal void ChangeType(WarehouseType newType) => WarehouseType = newType;

    internal void SetActive(bool isActive) => IsActive = isActive;

    #endregion
}
