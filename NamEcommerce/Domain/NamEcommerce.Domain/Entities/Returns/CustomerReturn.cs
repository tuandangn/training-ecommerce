using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Returns;

namespace NamEcommerce.Domain.Entities.Returns;

[Serializable]
public sealed record CustomerReturn : AppAggregateEntity
{
    private CustomerReturn() : base(Guid.Empty) { }

    internal CustomerReturn(string code, Guid orderId, string orderCode,
        Guid customerId, string customerName,
        Guid warehouseId, string warehouseName,
        string? note, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        OrderId = orderId;
        OrderCode = orderCode;
        CustomerId = customerId;
        CustomerName = customerName;
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        Note = note;
        CreatedByUserId = createdByUserId;
        Status = CustomerReturnStatus.Draft;
        ReturnDate = DateTime.UtcNow;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;

    public Guid OrderId { get; private set; }
    public string OrderCode { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;

    public Guid WarehouseId { get; private set; }
    public string WarehouseName { get; private set; } = string.Empty;

    public string? Note { get; internal set; }
    public CustomerReturnStatus Status { get; private set; }
    public DateTime ReturnDate { get; internal set; }

    public DateTime? ConfirmedOnUtc { get; private set; }

    /// <summary>ID phiếu nhập kho được tự động sinh khi Confirm (SourceType=FromCustomerReturn).</summary>
    public Guid? GeneratedGoodsReceiptId { get; internal set; }

    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly IList<CustomerReturnItem> _items = [];
    public IReadOnlyCollection<CustomerReturnItem> Items => _items.AsReadOnly();

    #region Methods

    internal void AddItem(Guid productId, string productName, Guid? deliveryNoteItemId,
        decimal requestedQuantity, decimal acceptedQuantity, decimal unitPrice)
    {
        if (requestedQuantity <= 0)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.RequestedQuantityMustBePositive");
        if (acceptedQuantity < 0)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.AcceptedQuantityCannotBeNegative");

        var item = new CustomerReturnItem(Guid.NewGuid(), Id, productId, productName,
            deliveryNoteItemId, requestedQuantity, acceptedQuantity, unitPrice);
        _items.Add(item);
    }

    internal void MoveToInspecting()
    {
        if (Status != CustomerReturnStatus.Draft)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(CustomerReturnStatus.Inspecting));

        Status = CustomerReturnStatus.Inspecting;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Confirm()
    {
        if (Status != CustomerReturnStatus.Inspecting)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(CustomerReturnStatus.Confirmed));

        if (!_items.Any())
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.NoItems");

        Status = CustomerReturnStatus.Confirmed;
        ConfirmedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CustomerReturnConfirmed(Id, OrderId, CustomerId, WarehouseId));
    }

    internal void Cancel()
    {
        if (Status is CustomerReturnStatus.Confirmed)
            throw new ReturnCannotChangeStatusException(Status.ToString(), nameof(CustomerReturnStatus.Cancelled));

        Status = CustomerReturnStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CustomerReturnCancelled(Id));
    }

    internal void MarkCreated() { /* no event needed at Draft creation */ }

    #endregion
}
