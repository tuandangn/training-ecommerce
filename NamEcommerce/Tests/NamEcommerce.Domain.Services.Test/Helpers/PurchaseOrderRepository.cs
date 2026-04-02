using NamEcommerce.Domain.Entities.PurchaseOrders;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class PurchaseOrderRepository
{
    public static Mock<IRepository<PurchaseOrder>> CreatePurchaseOrderWillReturns(PurchaseOrder @return)
        => Repository.Create<PurchaseOrder>().WhenCall(r =>
            r.InsertAsync(It.Is<PurchaseOrder>(entity =>
            entity.Code == @return.Code && entity.WarehouseId == @return.WarehouseId
            && entity.VendorId == @return.VendorId && entity.CreatedByUserId == @return.CreatedByUserId
            && entity.ExpectedDeliveryDateUtc == @return.ExpectedDeliveryDateUtc
            && entity.Note == @return.Note
            && entity.TaxAmount == @return.TaxAmount && entity.ShippingAmount == @return.ShippingAmount))
        , @return);

    public static Mock<IRepository<PurchaseOrder>> UpdatePurchaseOrderWillReturns(PurchaseOrder @return)
        => Repository.Create<PurchaseOrder>().WhenCall(r =>
            r.UpdateAsync(It.Is<PurchaseOrder>(entity =>
            entity.Code == @return.Code && entity.WarehouseId == @return.WarehouseId
            && entity.VendorId == @return.VendorId && entity.CreatedByUserId == @return.CreatedByUserId
            && entity.ExpectedDeliveryDateUtc == @return.ExpectedDeliveryDateUtc
            && entity.Note == @return.Note && entity.Status == @return.Status
            && entity.TaxAmount == @return.TaxAmount && entity.ShippingAmount == @return.ShippingAmount
            && entity.Items.Count() == @return.Items.Count() 
                && entity.Items.All(item => @return.Items.Any(rItem =>
                    rItem.ProductId == item.ProductId && rItem.QuantityOrdered == item.QuantityOrdered
                    && rItem.UnitCost == item.UnitCost && rItem.Note == item.Note
                ))
        )) , @return);

    public static Mock<IRepository<PurchaseOrder>> NotFound(Guid id)
        => Repository.Create<PurchaseOrder>().WhenCall(r => r.GetByIdAsync(id, default), (PurchaseOrder?)null);
    public static Mock<IRepository<PurchaseOrder>> NotFound(this Mock<IRepository<PurchaseOrder>> mock, Guid id)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), (PurchaseOrder?)null);

    public static Mock<IRepository<PurchaseOrder>> PurchaseOrderById(Guid id, PurchaseOrder @return)
        => Repository.Create<PurchaseOrder>().WhenCall(r => r.GetByIdAsync(id, default), @return);
    public static Mock<IRepository<PurchaseOrder>> PurchaseOrderById(this Mock<IRepository<PurchaseOrder>> mock, Guid id, PurchaseOrder @return)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<PurchaseOrder>> CanDeletePurchaseOrder(PurchaseOrder purchaseOrder)
        => Repository.Create<PurchaseOrder>().CanCall(r => r.DeleteAsync(purchaseOrder));
}
