using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class PurchaseOrderDataReader
{
    public static Mock<IEntityDataReader<PurchaseOrder>> Empty()
        => EntityDataReader.Create<PurchaseOrder>().WithData(Array.Empty<PurchaseOrder>());

    public static Mock<IEntityDataReader<PurchaseOrder>> WithData(params PurchaseOrder[] purchaseOrders)
        => EntityDataReader.Create<PurchaseOrder>().WithData(purchaseOrders);
    public static Mock<IEntityDataReader<PurchaseOrder>> HasOne(PurchaseOrder purchaseOrder)
        => EntityDataReader.Create<PurchaseOrder>().WithData(purchaseOrder);

    public static Mock<IEntityDataReader<PurchaseOrder>> NotFound(Guid id)
        => EntityDataReader.Create<PurchaseOrder>().WhenCall(reader => reader.GetByIdAsync(id), (PurchaseOrder?)null);

    public static Mock<IEntityDataReader<PurchaseOrder>> PurchaseOrderById(Guid id, PurchaseOrder purchaseOrder)
        => EntityDataReader.Create<PurchaseOrder>().WhenCall(reader => reader.GetByIdAsync(id), purchaseOrder);
    public static Mock<IEntityDataReader<PurchaseOrder>> PurchaseOrderById(this Mock<IEntityDataReader<PurchaseOrder>> mock, Guid id, PurchaseOrder purchaseOrder)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), purchaseOrder);
}
