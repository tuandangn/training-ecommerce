using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Returns;

public sealed class VendorReturnManager(
    IRepository<VendorReturn> vendorReturnRepository,
    IRepository<DeliveryNote> deliveryNoteRepository,
    IEntityDataReader<VendorReturn> vendorReturnDataReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Vendor> vendorDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IEntityDataReader<Expense> expenseDataReader,
    IInventoryStockManager inventoryStockManager,
    IInventoryCostingManager inventoryCostingManager,
    IExpenseManager expenseManager,
    IVendorDebtManager vendorDebtManager,
    ICurrentUserAccessor currentUserAccessor) : IVendorReturnManager
{
    public async Task<VendorReturnDto> CreateAsync(CreateVendorReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new ReturnDataIsInvalidException("Error.VendorReturn.VendorNotFound", dto.VendorId);

        GoodsReceipt? goodsReceipt = null;
        if (dto.GoodsReceiptId.HasValue)
        {
            goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId.Value).ConfigureAwait(false);
            if (goodsReceipt is null)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.GoodsReceiptNotFound", dto.GoodsReceiptId.Value);
        }

        Warehouse? warehouse = null;
        if (dto.WarehouseId.HasValue)
        {
            warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.WarehouseNotFound", dto.WarehouseId.Value);
        }

        var code = GenerateCode();
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);

        var vendorReturn = new VendorReturn(
            code: code,
            vendorId: vendor.Id,
            vendorName: vendor.Name,
            purchaseOrderId: goodsReceipt?.PurchaseOrderId,
            goodsReceiptId: dto.GoodsReceiptId,
            warehouseId: warehouse?.Id,
            warehouseName: warehouse?.Name,
            note: dto.Note,
            additionalCost: dto.AdditionalCost,
            createdByUserId: currentUser?.Id);

        foreach (var itemDto in dto.Items)
        {
            var product = await productDataReader.GetByIdAsync(itemDto.ProductId).ConfigureAwait(false);
            if (product is null)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.ProductNotFound", itemDto.ProductId);

            vendorReturn.AddItem(
                productId: itemDto.ProductId,
                productName: product.Name,
                goodsReceiptItemId: itemDto.GoodsReceiptItemId,
                requestedQuantity: itemDto.RequestedQuantity,
                acceptedQuantity: itemDto.AcceptedQuantity,
                originalUnitCost: itemDto.OriginalUnitCost,
                returnUnitCost: itemDto.ReturnUnitCost);
        }

        vendorReturn.MarkCreated();
        var inserted = await vendorReturnRepository.InsertAsync(vendorReturn).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task<VendorReturnDto> UpdateAsync(UpdateVendorReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(dto.Id);

        vendorReturn.Note = dto.Note;
        if (dto.ReturnDate.HasValue)
            vendorReturn.ReturnDate = dto.ReturnDate.Value;

        await vendorReturnRepository.UpdateAsync(vendorReturn).ConfigureAwait(false);
        return vendorReturn.ToDto();
    }

    public async Task MoveToInspectingAsync(Guid id)
    {
        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(id);

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        vendorReturn.MoveToInspecting(currentUser?.Id);
        await vendorReturnRepository.UpdateAsync(vendorReturn).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id, Guid? warehouseId = null)
    {
        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(id);

        if (warehouseId.HasValue)
        {
            if (warehouseId.Value == Guid.Empty)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.WarehouseRequired");

            var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new ReturnDataIsInvalidException("Error.VendorReturn.WarehouseNotFound", warehouseId.Value);

            vendorReturn.SetWarehouse(warehouse.Id, warehouse.Name);
        }

        // Validate: tổng AcceptedQuantity không được vượt quá số đã nhập từ NCC
        var acceptedByProduct = vendorReturn.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                AcceptedQuantity = group.Sum(item => item.AcceptedQuantity)
            });
        foreach (var item in acceptedByProduct)
        {
            var receivedQty = GetTotalReceivedQuantity(vendorReturn, item.ProductId);
            var previouslyReturned = await GetTotalConfirmedReturnQuantityAsync(
                item.ProductId,
                goodsReceiptId: vendorReturn.GoodsReceiptId,
                purchaseOrderId: vendorReturn.PurchaseOrderId,
                excludeReturnId: id).ConfigureAwait(false);
            var maxAllowed = receivedQty - previouslyReturned;

            if (item.AcceptedQuantity > maxAllowed)
                throw new ExceedsReceivedQuantityException(item.ProductId, item.AcceptedQuantity, maxAllowed);
        }

        vendorReturn.Confirm();
        await vendorReturnRepository.UpdateAsync(vendorReturn).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(id);

        vendorReturn.Cancel();
        await vendorReturnRepository.UpdateAsync(vendorReturn).ConfigureAwait(false);
    }

    public async Task ReverseConfirmedAsync(Guid id, string reason)
    {
        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(id);

        EnsureCanReverseVendorReturn(vendorReturn, reason);
        var generatedDeliveryNote = await GetGeneratedVendorReturnDeliveryAsync(vendorReturn).ConfigureAwait(false);

        foreach (var item in vendorReturn.Items)
        {
            if (item.AcceptedQuantity <= 0) continue;
            await inventoryStockManager.ReceiveStockUpToAsync(
                productId: item.ProductId,
                warehouseId: vendorReturn.WarehouseId,
                targetQuantity: vendorReturn.Items
                    .Where(i => i.ProductId == item.ProductId)
                    .Sum(i => i.AcceptedQuantity),
                note: $"Đảo ngược phiếu trả NCC {vendorReturn.Code}",
                receivedByUserId: vendorReturn.CreatedByUserId,
                referenceType: (int)NamEcommerce.Domain.Entities.Inventory.StockReferenceType.VendorReturn,
                referenceId: vendorReturn.Id).ConfigureAwait(false);

            await inventoryCostingManager.RegisterInboundAsync(new RegisterInventoryInboundCostDto
            {
                ProductId = item.ProductId,
                WarehouseId = vendorReturn.WarehouseId,
                Quantity = item.AcceptedQuantity,
                UnitCost = ResolveVendorReturnReversalUnitCost(generatedDeliveryNote, item),
                MovementType = InventoryCostMovementType.VendorReturnReversal,
                ReferenceType = InventoryCostReferenceType.VendorReturn,
                ReferenceId = vendorReturn.Id,
                ReferenceItemId = item.Id,
                OccurredAtUtc = DateTime.UtcNow
            }).ConfigureAwait(false);
        }

        if (generatedDeliveryNote is not null)
        {
            generatedDeliveryNote.ReverseVendorReturnDelivery();
            await deliveryNoteRepository.UpdateAsync(generatedDeliveryNote).ConfigureAwait(false);
        }

        var totalReturnAmount = Math.Max(0,
            vendorReturn.Items.Sum(i => i.AcceptedQuantity * i.ReturnUnitCost) - vendorReturn.AdditionalCost);
        if (totalReturnAmount > 0)
            await vendorDebtManager.ReverseReturnFromVendorReturnAsync(
                vendorReturn.GoodsReceiptId,
                vendorReturn.PurchaseOrderId,
                totalReturnAmount).ConfigureAwait(false);

        var linkedExpense = expenseDataReader.DataSource
            .FirstOrDefault(e => e.SourceVendorReturnId == vendorReturn.Id);
        if (linkedExpense is not null)
            await expenseManager.DeleteExpenseAsync(linkedExpense.Id).ConfigureAwait(false);

        vendorReturn.MarkReversed(reason);
        await vendorReturnRepository.UpdateAsync(vendorReturn).ConfigureAwait(false);
    }

    public async Task<VendorReturnDto?> GetByIdAsync(Guid id)
    {
        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return vendorReturn?.ToDto();
    }

    public Task<(int Total, List<VendorReturnDto> Items)> GetListAsync(
        Guid? vendorId, Guid? purchaseOrderId, Guid? goodsReceiptId, int? status, int pageIndex, int pageSize)
    {
        var query = vendorReturnDataReader.DataSource;

        if (vendorId.HasValue)
            query = query.Where(r => r.VendorId == vendorId.Value);
        if (purchaseOrderId.HasValue)
            query = query.Where(r => r.PurchaseOrderId == purchaseOrderId.Value);
        if (goodsReceiptId.HasValue)
            query = query.Where(r => r.GoodsReceiptId == goodsReceiptId.Value);
        if (status.HasValue)
            query = query.Where(r => (int)r.Status == status.Value);

        var total = query.Count();
        var items = query
            .OrderByDescending(r => r.CreatedOnUtc)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList()
            .Select(r => r.ToDto())
            .ToList();

        return Task.FromResult((total, items));
    }

    public Task<decimal> GetTotalConfirmedReturnQuantityAsync(
        Guid productId, Guid? goodsReceiptId, Guid? purchaseOrderId, Guid? excludeReturnId = null)
    {
        var query = vendorReturnDataReader.DataSource
            .Where(r => (int)r.Status == 2 // Confirmed
                        && (excludeReturnId == null || r.Id != excludeReturnId));

        if (goodsReceiptId.HasValue)
            query = query.Where(r => r.GoodsReceiptId == goodsReceiptId.Value);
        else if (purchaseOrderId.HasValue)
            query = query.Where(r => r.PurchaseOrderId == purchaseOrderId.Value);

        decimal total = 0;
        var confirmedReturns = query.ToList();
        foreach (var ret in confirmedReturns)
            total += ret.Items
                .Where(i => i.ProductId == productId)
                .Sum(i => i.AcceptedQuantity);

        return Task.FromResult(total);
    }

    public async Task FinalizeConfirmAsync(Guid returnId, Guid generatedDeliveryNoteId, decimal totalReturnAmount)
    {
        var vendorReturn = await vendorReturnDataReader.GetByIdAsync(returnId).ConfigureAwait(false);
        if (vendorReturn is null) return;

        // Idempotency guard
        if (vendorReturn.GeneratedDeliveryNoteId.HasValue) return;

        vendorReturn.GeneratedDeliveryNoteId = generatedDeliveryNoteId;
        await vendorReturnRepository.UpdateAsync(vendorReturn).ConfigureAwait(false);

        if (vendorReturn.AdditionalCost > 0)
        {
            await expenseManager.CreateExpenseAsync(new CreateExpenseDto
            {
                Title = $"Chi phí phát sinh phiếu trả NCC {vendorReturn.Code}",
                Description = $"Chi phí phát sinh khi trả hàng cho nhà cung cấp {vendorReturn.VendorName}",
                Amount = vendorReturn.AdditionalCost,
                ExpenseType = ExpenseType.ReturnCost,
                IncurredDate = vendorReturn.ConfirmedOnUtc ?? DateTime.UtcNow,
                SourceVendorReturnId = vendorReturn.Id
            }).ConfigureAwait(false);
        }

        if (totalReturnAmount <= 0) return;

        await vendorDebtManager.ApplyReturnFromVendorReturnAsync(
            returnId,
            vendorReturn.GoodsReceiptId,
            vendorReturn.PurchaseOrderId,
            totalReturnAmount).ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private string GenerateCode()
    {
        var datePrefix = $"TNCC-{DateTime.UtcNow:yyyyMMdd}";
        var count = vendorReturnDataReader.DataSource.Count(r => r.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
    }

    private static void EnsureCanReverseVendorReturn(VendorReturn vendorReturn, string reason)
    {
        if (vendorReturn.Status != VendorReturnStatus.Confirmed)
            throw new ReturnCannotChangeStatusException(
                vendorReturn.Status.ToString(),
                nameof(VendorReturnStatus.Reversed));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ReturnDataIsInvalidException("Error.VendorReturn.ReverseReasonRequired");
    }

    private async Task<DeliveryNote?> GetGeneratedVendorReturnDeliveryAsync(VendorReturn vendorReturn)
    {
        if (!vendorReturn.GeneratedDeliveryNoteId.HasValue)
            return null;

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(vendorReturn.GeneratedDeliveryNoteId.Value)
            .ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(vendorReturn.GeneratedDeliveryNoteId.Value);
        if (deliveryNote.SourceType != DeliveryNoteSourceType.ToVendorReturn)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, deliveryNote.Status);
        if (deliveryNote.Status != DeliveryNoteStatus.Delivered)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, DeliveryNoteStatus.Cancelled);

        return deliveryNote;
    }

    private static decimal? ResolveVendorReturnReversalUnitCost(DeliveryNote? deliveryNote, VendorReturnItem item)
    {
        var relatedDeliveryItems = deliveryNote?.Items
            .Where(deliveryItem => deliveryItem.ProductId == item.ProductId && deliveryItem.CostAtDispatch.HasValue)
            .ToList();

        if (relatedDeliveryItems is { Count: > 0 })
        {
            var totalQuantity = relatedDeliveryItems.Sum(deliveryItem => deliveryItem.Quantity);
            if (totalQuantity > 0)
            {
                var totalCost = relatedDeliveryItems.Sum(
                    deliveryItem => deliveryItem.Quantity * deliveryItem.CostAtDispatch!.Value);
                return totalCost / totalQuantity;
            }
        }

        return item.OriginalUnitCost ?? item.ReturnUnitCost;
    }

    /// <summary>
    /// Tính tổng số lượng đã nhập từ NCC cho một productId,
    /// lọc theo GoodsReceiptId (nếu có) hoặc PurchaseOrderId (nếu có).
    /// </summary>
    private decimal GetTotalReceivedQuantity(VendorReturn vendorReturn, Guid productId)
    {
        if (vendorReturn.GoodsReceiptId.HasValue)
        {
            // Lọc trực tiếp theo GoodsReceipt cụ thể
            return goodsReceiptDataReader.DataSource
                .Where(gr => gr.Id == vendorReturn.GoodsReceiptId.Value)
                .ToList()
                .SelectMany(gr => gr.Items.Where(item => item.ProductId == productId))
                .Sum(i => i.Quantity);
        }

        if (vendorReturn.PurchaseOrderId.HasValue)
        {
            // Lấy tất cả GoodsReceipt gắn với PurchaseOrderId
            var goodsReceipts = goodsReceiptDataReader.DataSource
                .Where(gr => gr.PurchaseOrderId == vendorReturn.PurchaseOrderId.Value)
                .ToList();

            if (!goodsReceipts.Any())
                return 0;

            return goodsReceipts
                .SelectMany(gr => gr.Items.Where(item => item.ProductId == productId))
                .Sum(i => i.Quantity);
        }

        return 0;
    }
}
