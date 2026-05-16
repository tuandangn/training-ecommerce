using NamEcommerce.Application.Contracts.Dtos.Dashboard;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;

namespace NamEcommerce.Application.Services.Extensions;

public static class DashboardExtensions
{
    public static PendingOrderAppDto ToPendingOrderAppDto(this Order order, string customerName)
        => new()
        {
            Id = order.Id,
            Code = order.Code,
            CustomerName = customerName,
            TotalAmount = order.OrderTotal,
            PendingItemCount = order.OrderItems.Count(item => !item.IsDelivered),
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            CreatedOnUtc = order.CreatedOnUtc
        };

    public static PendingPurchaseOrderAppDto ToPendingPurchaseOrderAppDto(this PurchaseOrder purchaseOrder, string vendorName)
        => new()
        {
            Id = purchaseOrder.Id,
            Code = purchaseOrder.Code,
            VendorName = vendorName,
            TotalAmount = purchaseOrder.TotalAmount,
            RemainingQuantity = purchaseOrder.Items.Sum(item => Math.Max(0, item.QuantityOrdered - item.QuantityReceived)),
            ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
            CreatedOnUtc = purchaseOrder.CreatedOnUtc
        };

    public static TopCustomerDebtAppDto ToTopCustomerDebtAppDto(
        this IEnumerable<CustomerDebt> debts,
        Guid customerId,
        string customerName)
        => new()
        {
            CustomerId = customerId,
            CustomerName = customerName,
            TotalRemainingAmount = debts.Sum(debt => debt.RemainingAmount)
        };

    public static TopVendorDebtAppDto ToTopVendorDebtAppDto(
        this IEnumerable<VendorDebt> debts,
        Guid vendorId,
        string vendorName)
        => new()
        {
            VendorId = vendorId,
            VendorName = vendorName,
            TotalRemainingAmount = debts.Sum(debt => debt.RemainingAmount)
        };

    public static LowStockProductAppDto ToLowStockProductAppDto(
        this InventoryStock stock,
        string productName,
        string warehouseName)
        => new()
        {
            InventoryStockId = stock.Id,
            ProductId = stock.ProductId,
            ProductName = productName,
            WarehouseId = stock.WarehouseId,
            WarehouseName = warehouseName,
            QuantityOnHand = stock.QuantityOnHand,
            QuantityReserved = stock.QuantityReserved,
            QuantityAvailable = stock.QuantityAvailable,
            ReorderLevel = stock.ReorderLevel
        };
}
