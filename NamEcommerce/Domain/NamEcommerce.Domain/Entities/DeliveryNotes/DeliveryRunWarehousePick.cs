using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.DeliveryNotes;

[Serializable]
public sealed record DeliveryRunWarehousePick : AppEntity
{
    private DeliveryRunWarehousePick(Guid id) : base(id)
    {
    }

    internal DeliveryRunWarehousePick(Guid deliveryRunId, Guid warehouseId,
        Guid? confirmedByUserId, string? confirmedByFullName, DateTime confirmedOnUtc) : base(Guid.Empty)
    {
        DeliveryRunId = deliveryRunId;
        WarehouseId = warehouseId;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedByFullName = confirmedByFullName;
        ConfirmedOnUtc = confirmedOnUtc;
    }

    public Guid DeliveryRunId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public string? ConfirmedByFullName { get; private set; }
    public DateTime ConfirmedOnUtc { get; private set; }
}
