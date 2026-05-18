using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.StockTransfer;
using NamEcommerce.Domain.Shared.Events.StockTransfer;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.StockTransfer;

namespace NamEcommerce.Domain.Entities.StockTransfer;

[Serializable]
public sealed record StockTransferNote : AppAggregateEntity
{
    public StockTransferNote(Guid id) : base(id)
    {
        Code = string.Empty;
        _items = [];
    }

    internal StockTransferNote(string code, Guid fromWarehouseId, string? fromWarehouseName,
        Guid toWarehouseId, string? toWarehouseName, string? note, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        FromWarehouseId = fromWarehouseId;
        FromWarehouseName = fromWarehouseName;
        ToWarehouseId = toWarehouseId;
        ToWarehouseName = toWarehouseName;
        Note = note;
        Status = StockTransferStatus.Draft;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
        _items = [];
    }

    public string Code { get; private set; }
    public Guid FromWarehouseId { get; private set; }
    public string? FromWarehouseName { get; private set; }
    public Guid ToWarehouseId { get; private set; }
    public string? ToWarehouseName { get; private set; }
    public string? Note { get; internal set; }
    public StockTransferStatus Status { get; private set; }
    public DateTime? ApprovedOnUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly List<StockTransferNoteItem> _items;
    public IReadOnlyCollection<StockTransferNoteItem> Items => _items.AsReadOnly();

    internal void AddItem(Guid productId, string productName, decimal quantity)
    {
        _items.Add(new StockTransferNoteItem(Id, productId, productName, quantity));
    }

    internal void Approve()
    {
        if (Status != StockTransferStatus.Draft)
            throw new StockTransferNoteCannotChangeStatusException(Status.ToString(), nameof(StockTransferStatus.Approved));

        if (_items.Count == 0)
            throw new NamEcommerceDomainException("Error.StockTransfer.NoItems");

        Status = StockTransferStatus.Approved;
        ApprovedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new StockTransferNoteApproved(Id, FromWarehouseId, ToWarehouseId));
    }

    internal void Cancel()
    {
        if (Status == StockTransferStatus.Approved)
            throw new StockTransferNoteCannotChangeStatusException(Status.ToString(), nameof(StockTransferStatus.Cancelled));

        Status = StockTransferStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
