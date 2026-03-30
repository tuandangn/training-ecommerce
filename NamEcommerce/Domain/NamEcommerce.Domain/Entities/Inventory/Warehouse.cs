using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record Warehouse : AppAggregateEntity
{
    internal Warehouse(string code, string name, WarehouseType warehouseType) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        ArgumentException.ThrowIfNullOrEmpty(name);

        Code = code;
        Name = name;
        WarehouseType = warehouseType;
        IsActive = true;

        NormalizedName = TextHelper.Normalize(Name);
        NormalizedAddress = TextHelper.Normalize(Address);
    }

    public string Code { get; internal set; }
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

    internal async Task SetNameAsync(string name, IExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new WarehouseNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    internal void ChangeType(WarehouseType newType) => WarehouseType = newType;

    internal void SetActive(bool isActive) => IsActive = isActive;
}

public enum WarehouseType
{
    Main,
    SubWarehouse,
    ReturnWarehouse
}
