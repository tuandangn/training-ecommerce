using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Customers;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Web.Models.E2E;

namespace NamEcommerce.Web.Services.E2E;

public sealed class E2ETestDataService(
    NamEcommerceEfDbContext dbContext,
    ICustomerAppService customerAppService,
    IVendorAppService vendorAppService,
    IWarehouseAppService warehouseAppService,
    IProductAppService productAppService) : IE2ETestDataService
{
    private const decimal UnitPrice = 150000m;
    private const decimal UnitCost = 100000m;

    public async Task ResetAsync(string? scenarioId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            return;

        var ids = await GetScenarioIdsAsync(scenarioId, cancellationToken).ConfigureAwait(false);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await dbContext.Set<CustomerPaymentIntent>()
            .Where(x => ids.CustomerIds.Contains(x.CustomerId) || x.CustomerDebtId.HasValue && ids.CustomerDebtIds.Contains(x.CustomerDebtId.Value))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<CustomerPayment>()
            .Where(x => ids.CustomerIds.Contains(x.CustomerId)
                        || x.OrderId.HasValue && ids.OrderIds.Contains(x.OrderId.Value)
                        || x.DeliveryNoteId.HasValue && ids.DeliveryNoteIds.Contains(x.DeliveryNoteId.Value)
                        || x.CustomerDebtId.HasValue && ids.CustomerDebtIds.Contains(x.CustomerDebtId.Value))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<VendorPayment>()
            .Where(x => ids.VendorIds.Contains(x.VendorId)
                        || x.PurchaseOrderId.HasValue && ids.PurchaseOrderIds.Contains(x.PurchaseOrderId.Value)
                        || x.VendorDebtId.HasValue && ids.VendorDebtIds.Contains(x.VendorDebtId.Value))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.Set<CustomerDebt>()
            .Where(x => ids.CustomerDebtIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<VendorDebt>()
            .Where(x => ids.VendorDebtIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.Set<PurchaseOrderItemAllocation>()
            .Where(x => ids.PurchaseOrderItemIds.Contains(x.PurchaseOrderItemId) || ids.OrderItemIds.Contains(x.OrderItemId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.Set<InventoryCostAllocation>()
            .Where(x => ids.ProductIds.Contains(x.ProductId) || ids.DeliveryNoteIds.Contains(x.OutboundReferenceId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<InventoryCostLayer>()
            .Where(x => ids.ProductIds.Contains(x.ProductId) || ids.GoodsReceiptIds.Contains(x.SourceReferenceId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<InventoryCostLedgerEntry>()
            .Where(x => ids.ProductIds.Contains(x.ProductId)
                        || ids.GoodsReceiptIds.Contains(x.ReferenceId)
                        || ids.DeliveryNoteIds.Contains(x.ReferenceId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<ProductReservationLedger>()
            .Where(x => ids.ProductIds.Contains(x.ProductId) || ids.OrderIds.Contains(x.OrderId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<StockMovementLog>()
            .Where(x => ids.ProductIds.Contains(x.ProductId)
                        || ids.GoodsReceiptIds.Contains(x.ReferenceId ?? Guid.Empty)
                        || ids.DeliveryNoteIds.Contains(x.ReferenceId ?? Guid.Empty))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<StockAuditLog>()
            .Where(x => ids.ProductIds.Contains(x.ProductId) || ids.WarehouseIds.Contains(x.WarehouseId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<InventoryStock>()
            .Where(x => ids.ProductIds.Contains(x.ProductId) || ids.WarehouseIds.Contains(x.WarehouseId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.Set<DeliveryNote>()
            .Where(x => ids.DeliveryNoteIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<GoodsReceipt>()
            .Where(x => ids.GoodsReceiptIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<PurchaseOrder>()
            .Where(x => ids.PurchaseOrderIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<Order>()
            .Where(x => ids.OrderIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await dbContext.Set<ProductPriceHistory>()
            .Where(x => ids.ProductIds.Contains(x.ProductId))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<Product>()
            .Where(x => ids.ProductIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<Warehouse>()
            .Where(x => ids.WarehouseIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<Vendor>()
            .Where(x => ids.VendorIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
        await dbContext.Set<Customer>()
            .Where(x => ids.CustomerIds.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<E2ESeedOrderWorkflowResult> SeedOrderWorkflowAsync(
        E2ESeedOrderWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScenarioId);

        await ResetAsync(request.ScenarioId, cancellationToken).ConfigureAwait(false);

        var marker = GetMarker(request.ScenarioId);
        var customerName = $"{marker} Customer";
        var vendorName = $"{marker} Vendor";
        var warehouseName = $"{marker} Warehouse";
        var productName = $"{marker} Product";
        var customerPhone = "0900000001";
        var shippingAddress = $"{marker} Shipping Address";

        var customerId = await CreateCustomerAsync(customerName, customerPhone, shippingAddress).ConfigureAwait(false);
        var vendorId = await CreateVendorAsync(vendorName).ConfigureAwait(false);
        var warehouseCode = $"E2E{StableSuffix(request.ScenarioId)}";
        await CreateWarehouseAsync(warehouseCode, warehouseName).ConfigureAwait(false);
        await CreateProductAsync(productName, vendorId).ConfigureAwait(false);

        return new E2ESeedOrderWorkflowResult
        {
            ScenarioId = request.ScenarioId,
            Quantity = request.Quantity,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            ShippingAddress = shippingAddress,
            VendorName = vendorName,
            WarehouseName = warehouseName,
            ProductName = productName,
            UnitPrice = UnitPrice,
            UnitCost = UnitCost
        };
    }

    public async Task<E2EOrderWorkflowState> GetOrderWorkflowStateAsync(
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);

        var ids = await GetScenarioIdsAsync(scenarioId, cancellationToken).ConfigureAwait(false);
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .Where(x => ids.OrderIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var purchaseOrder = await dbContext.Set<PurchaseOrder>()
            .AsNoTracking()
            .Where(x => ids.PurchaseOrderIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var deliveryNote = await dbContext.Set<DeliveryNote>()
            .AsNoTracking()
            .Where(x => ids.DeliveryNoteIds.Contains(x.Id))
            .OrderByDescending(x => x.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var inventoryStock = await dbContext.Set<InventoryStock>()
            .AsNoTracking()
            .Where(stock => ids.ProductIds.Contains(stock.ProductId) && ids.WarehouseIds.Contains(stock.WarehouseId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var productReservationLedgerEntries = await dbContext.Set<ProductReservationLedger>()
            .AsNoTracking()
            .Where(entry => ids.ProductIds.Contains(entry.ProductId) && ids.OrderIds.Contains(entry.OrderId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new E2EOrderWorkflowState
        {
            ScenarioId = scenarioId,
            OrderCode = order?.Code,
            PurchaseOrderCode = purchaseOrder?.Code,
            DeliveryNoteCode = deliveryNote?.Code,
            OrderStatus = order?.OrderStatus.ToString() ?? "Missing",
            PurchaseOrderStatus = purchaseOrder?.Status.ToString() ?? "Missing",
            DeliveryStatus = deliveryNote?.Status.ToString() ?? "Missing",
            OrderedQuantity = order?.OrderItems.Sum(x => x.Quantity) ?? 0,
            ReceivedQuantity = purchaseOrder?.Items.Sum(x => x.QuantityReceived) ?? 0,
            DeliveredQuantity = deliveryNote?.Items.Sum(x => x.Quantity) ?? 0,
            StockInfo = new E2EInventoryStockState
            {
                ScenarioId = scenarioId,
                GlobalReservedQuantity = productReservationLedgerEntries.Sum(entry => entry.QuantityDelta),
                StockOnHandQuantity = inventoryStock?.QuantityOnHand ?? 0,
                StockReservedQuantity = inventoryStock?.QuantityReserved ?? 0,
                StockAvailableQuantity = inventoryStock?.QuantityAvailable ?? 0
            }
        };
    }

    private async Task<Guid> CreateCustomerAsync(string name, string phone, string address)
    {
        var result = await customerAppService.CreateCustomerAsync(new CreateCustomerAppDto
        {
            FullName = name,
            PhoneNumber = phone,
            Address = address
        }).ConfigureAwait(false);

        if (!result.Success || !result.CreatedId.HasValue)
            throw new InvalidOperationException(result.ErrorMessage ?? "Cannot create E2E customer.");

        return result.CreatedId.Value;
    }

    private async Task<Guid> CreateVendorAsync(string name)
    {
        var result = await vendorAppService.CreateVendorAsync(new CreateVendorAppDto
        {
            Name = name,
            PhoneNumber = "0900000002",
            Address = $"{name} Address",
            DisplayOrder = 0
        }).ConfigureAwait(false);

        if (!result.Success || !result.CreatedId.HasValue)
            throw new InvalidOperationException(result.ErrorMessage ?? "Cannot create E2E vendor.");

        return result.CreatedId.Value;
    }

    private async Task<Guid> CreateWarehouseAsync(string code, string name)
    {
        var result = await warehouseAppService.CreateWarehouseAsync(new CreateWarehouseAppDto
        {
            Code = code,
            Name = name,
            WarehouseType = (int)WarehouseType.Physical,
            PhoneNumber = "0900000003",
            Address = $"{name} Address",
            IsActive = true
        }).ConfigureAwait(false);

        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? "Cannot create E2E warehouse.");

        return result.CreatedId;
    }

    private async Task<Guid> CreateProductAsync(string name, Guid vendorId)
    {
        var result = await productAppService.CreateProductAsync(new CreateProductAppDto
        {
            Name = name,
            ShortDesc = name,
            CostPrice = UnitCost,
            UnitPrice = UnitPrice,
            Vendors = [new ProductVendorAppDto(vendorId, 0)]
        }).ConfigureAwait(false);

        if (!result.Success || !result.CreatedId.HasValue)
            throw new InvalidOperationException(result.ErrorMessage ?? "Cannot create E2E product.");

        return result.CreatedId.Value;
    }

    private async Task<ScenarioIds> GetScenarioIdsAsync(string scenarioId, CancellationToken cancellationToken)
    {
        var marker = GetMarker(scenarioId);

        var customerIds = await dbContext.Set<Customer>()
            .IgnoreQueryFilters()
            .Where(x => x.FullName.Value.Contains(marker))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var vendorIds = await dbContext.Set<Vendor>()
            .IgnoreQueryFilters()
            .Where(x => x.Name.Contains(marker))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var warehouseIds = await dbContext.Set<Warehouse>()
            .IgnoreQueryFilters()
            .Where(x => x.Name.Value.Contains(marker))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var productIds = await dbContext.Set<Product>()
            .IgnoreQueryFilters()
            .Where(x => x.Name.Contains(marker))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var orderIds = await dbContext.Set<Order>()
            .IgnoreQueryFilters()
            .Where(x => customerIds.Contains(x.CustomerId) || x.OrderItems.Any(i => productIds.Contains(i.ProductId)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var orderItemIds = await dbContext.Set<OrderItem>()
            .Where(x => orderIds.Contains(x.OrderId) || productIds.Contains(x.ProductId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var purchaseOrderIds = await dbContext.Set<PurchaseOrder>()
            .IgnoreQueryFilters()
            .Where(x => vendorIds.Contains(x.VendorId)
                        || x.WarehouseId.HasValue && warehouseIds.Contains(x.WarehouseId.Value)
                        || x.Items.Any(i => productIds.Contains(i.ProductId)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var purchaseOrderItemIds = await dbContext.Set<PurchaseOrderItem>()
            .Where(x => purchaseOrderIds.Contains(x.PurchaseOrderId) || productIds.Contains(x.ProductId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var deliveryNoteIds = await dbContext.Set<DeliveryNote>()
            .IgnoreQueryFilters()
            .Where(x => orderIds.Contains(x.OrderId)
                        || customerIds.Contains(x.CustomerId)
                        || x.Items.Any(i => productIds.Contains(i.ProductId)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var goodsReceiptIds = await dbContext.Set<GoodsReceipt>()
            .IgnoreQueryFilters()
            .Where(x => x.PurchaseOrderId.HasValue && purchaseOrderIds.Contains(x.PurchaseOrderId.Value)
                        || x.VendorId.HasValue && vendorIds.Contains(x.VendorId.Value)
                        || x.Items.Any(i => productIds.Contains(i.ProductId)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var customerDebtIds = await dbContext.Set<CustomerDebt>()
            .IgnoreQueryFilters()
            .Where(x => customerIds.Contains(x.CustomerId)
                        || orderIds.Contains(x.OrderId)
                        || deliveryNoteIds.Contains(x.DeliveryNoteId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var vendorDebtIds = await dbContext.Set<VendorDebt>()
            .IgnoreQueryFilters()
            .Where(x => vendorIds.Contains(x.VendorId)
                        || x.PurchaseOrderId.HasValue && purchaseOrderIds.Contains(x.PurchaseOrderId.Value)
                        || x.GoodsReceiptId.HasValue && goodsReceiptIds.Contains(x.GoodsReceiptId.Value))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var productReservationLedgerIds = await dbContext.Set<ProductReservationLedger>()
            .IgnoreQueryFilters()
            .Where(x => productIds.Contains(x.ProductId)
                        || orderIds.Contains(x.OrderId))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ScenarioIds(
            customerIds,
            vendorIds,
            warehouseIds,
            productIds,
            orderIds,
            orderItemIds,
            purchaseOrderIds,
            purchaseOrderItemIds,
            deliveryNoteIds,
            goodsReceiptIds,
            customerDebtIds,
            vendorDebtIds,
            productReservationLedgerIds
        );
    }

    private static string GetMarker(string scenarioId) => $"E2E-{scenarioId}";

    private static string StableSuffix(string scenarioId)
        => new(scenarioId.Where(char.IsLetterOrDigit).TakeLast(8).ToArray());

    public async Task<E2EInventoryStockState> GetInventoryStockStateAsync(string scenarioId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);

        var ids = await GetScenarioIdsAsync(scenarioId, cancellationToken).ConfigureAwait(false);
        var inventoryStock = await dbContext.Set<InventoryStock>()
            .AsNoTracking()
            .Where(stock => ids.ProductIds.Contains(stock.ProductId) && ids.WarehouseIds.Contains(stock.WarehouseId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        var productReservationLedgers = await dbContext.Set<ProductReservationLedger>()
            .AsNoTracking()
            .Where(entry => ids.ProductIds.Contains(entry.ProductId) && ids.OrderIds.Contains(entry.OrderId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new E2EInventoryStockState
        {
            ScenarioId = scenarioId,
            StockOnHandQuantity = inventoryStock?.QuantityOnHand ?? 0,
            StockReservedQuantity = inventoryStock?.QuantityReserved ?? 0,
            StockAvailableQuantity = inventoryStock?.QuantityAvailable ?? 0,
            GlobalReservedQuantity = productReservationLedgers.Sum(item => item.QuantityDelta)
        };
    }

    private sealed record ScenarioIds(
        List<Guid> CustomerIds,
        List<Guid> VendorIds,
        List<Guid> WarehouseIds,
        List<Guid> ProductIds,
        List<Guid> OrderIds,
        List<Guid> OrderItemIds,
        List<Guid> PurchaseOrderIds,
        List<Guid> PurchaseOrderItemIds,
        List<Guid> DeliveryNoteIds,
        List<Guid> GoodsReceiptIds,
        List<Guid> CustomerDebtIds,
        List<Guid> VendorDebtIds,
        List<Guid> ProductReservationLedgerIds
    );
}
