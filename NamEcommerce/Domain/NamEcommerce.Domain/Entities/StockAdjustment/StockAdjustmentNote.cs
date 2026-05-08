using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.StockAdjustment;
using NamEcommerce.Domain.Shared.Events.StockAdjustment;
using NamEcommerce.Domain.Shared.Exceptions.StockAdjustment;

namespace NamEcommerce.Domain.Entities.StockAdjustment;

[Serializable]
public sealed record StockAdjustmentNote : AppAggregateEntity
{
    public StockAdjustmentNote(Guid id) : base(id)
    {
        Code = string.Empty;
        _items = [];
    }

    internal StockAdjustmentNote(string code, Guid warehouseId, string? warehouseName,
        string? note, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        Note = note;
        Status = StockAdjustmentStatus.Draft;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
        _items = [];
    }

    public string Code { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string? WarehouseName { get; private set; }
    public string? Note { get; internal set; }
    public StockAdjustmentStatus Status { get; private set; }
    public DateTime? ApprovedOnUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private readonly List<StockAdjustmentNoteItem> _items;
    public IReadOnlyCollection<StockAdjustmentNoteItem> Items => _items.AsReadOnly();

    internal void AddItem(Guid productId, string productName, decimal systemQuantity, decimal physicalQuantity)
    {
        _items.Add(new StockAdjustmentNoteItem(Id, productId, productName, systemQuantity, physicalQuantity));
    }

    internal void Approve()
    {
        if (Status != StockAdjustmentStatus.Draft)
            throw new StockAdjustmentNoteCannotChangeStatusException(Status.ToString(), nameof(StockAdjustmentStatus.Approved));

        if (_items.Count == 0)
            throw new NamEcommerce.Domain.Shared.Exceptions.NamEcommerceDomainException("Error.StockAdjustment.NoItems");

        Status = StockAdjustmentStatus.Approved;
        ApprovedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new StockAdjustmentNoteApproved(Id, WarehouseId));
    }

    internal void Cancel()
    {
        if (Status == StockAdjustmentStatus.Approved)
            throw new StockAdjustmentNoteCannotChangeStatusException(Status.ToString(), nameof(StockAdjustmentStatus.Cancelled));

        Status = StockAdjustmentStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
