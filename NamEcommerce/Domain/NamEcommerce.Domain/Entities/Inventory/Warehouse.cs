using NamEcommerce.Domain.Metadata;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record Warehouse : AppAggregateEntity
{
    public const string CODE_PREFIX = "KH";

    internal Warehouse(WarehouseType warehouseType) : base(Guid.NewGuid()) 
        => WarehouseType = warehouseType;

    public string Code { get; private set; } = "";
    public NormalizableString Name { get; private set; }
    public NormalizableString Address { get; internal set; }
    public string? PhoneNumber { get; internal set; }
    public Guid? ManagerUserId { get; internal set; }
    public WarehouseType WarehouseType { get; private set; }
    public int DisplayOrder { get; internal set; }
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
    internal bool IsDirectTransit() => WarehouseType == WarehouseType.DirectTransit;

    internal void SetActive(bool isActive) => IsActive = isActive;

    #endregion

    #region Events

    internal void MarkCreated()
        => RaiseDomainEvent(new WarehouseCreated(Id, Code, Name));

    internal void MarkUpdated()
        => RaiseDomainEvent(new WarehouseUpdated(Id));

    internal void MarkDeleted()
        => RaiseDomainEvent(new WarehouseDeleted(Id, Code, Name));

    #endregion
}
