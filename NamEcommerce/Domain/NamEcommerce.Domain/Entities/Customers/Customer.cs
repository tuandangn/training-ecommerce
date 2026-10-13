using NamEcommerce.Domain.Metadata;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Events.Customers;

namespace NamEcommerce.Domain.Entities.Customers;

[Serializable]
public sealed record Customer : AppAggregateEntity
{
    private Customer() : base(Guid.NewGuid()) { }
    internal Customer(Guid id, string fullName, string phoneNumber, string address): base(id)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Address = address;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public NormalizableString FullName { get; internal set; }
    public NormalizableString Address { get; internal set; }
    public string PhoneNumber { get; internal set; } = string.Empty;
    public string? Email { get; internal set; }
    public string? Note { get; internal set; }

    public CustomerKind Kind { get; private set; }
    public bool IsSystem { get; internal set; }

    public DateTime CreatedOnUtc { get; init; }

    #region Methods

    public bool IsRetailWalkIn() => Kind == CustomerKind.RetailWalkIn;
    internal void SetCustomerKind(CustomerKind kind)
    {
        var oldKind = Kind;
        if (kind == oldKind)
            return;

        Kind = kind;
        MarkCustomerKindChanged(oldKind);
    }

    #endregion

    #region Events

    internal void MarkCreated()
        => RaiseDomainEvent(new CustomerCreated(Id, FullName, PhoneNumber));

    internal void MarkUpdated()
        => RaiseDomainEvent(new CustomerUpdated(Id));

    internal void MarkDeleted()
        => RaiseDomainEvent(new CustomerDeleted(Id, FullName));

    internal void MarkCustomerKindChanged(CustomerKind oldKind)
        => RaiseDomainEvent(new CustomerKindChanged(Id, Kind, oldKind));

    #endregion
}
