using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Domain.Services.GoodsReceipts;

public sealed class GoodsReceiptManager(
    IRepository<GoodsReceipt> goodsReceiptRepository,
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    IEntityDataReader<Product> productDataReader,
    WarehouseSettings warehouseSettings,
    IEntityDataReader<Warehouse> warehouseDataReader,
    ICurrentUserAccessor currentUserAccessor,
    IEntityDataReader<Picture> pictureDataReader,
    IEntityDataReader<Vendor> vendorDataReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
    IInventoryStockManager inventoryStockManager,
    IEntityDataReader<VendorReturn> vendorReturnReader,
    IEntityDataReader<CustomerReturn> customerReturnReader,
    IVendorReturnManager vendorReturnManager,
    IVendorDebtManager vendorDebtManager,
    IEntityDataReader<VendorDebt> vendorDebtReader) : IGoodsReceiptManager
{
    private Task<string> GenerateCodeAsync()
    {
        var monthPrefix = $"{GoodsReceipt.CODE_PREFIX}-{DateTime.UtcNow:yyMM}";
        var count = goodsReceiptDataReader.SecuredDataSource.Count(d => d.Code.StartsWith(monthPrefix));
        return Task.FromResult($"{monthPrefix}-{(count + 1):D3}");
    }

    public async Task<CreateGoodsReceiptResultDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await GoodsReceipt.CreateAsync(Guid.NewGuid(), currentUserAccessor, goodsReceiptDataReader);
        goodsReceipt.SetCode(await GenerateCodeAsync().ConfigureAwait(false));
        goodsReceipt.SetReceivedDate(dto.ReceivedOnUtc);
        goodsReceipt.TruckDriverName = dto.TruckDriverName;
        goodsReceipt.TruckNumberSerial = dto.TruckNumberSerial;
        foreach (var item in dto.Items)
            await goodsReceipt.AddItemAsync(item.ProductId, item.WarehouseId, item.Quantity, item.UnitCost, productDataReader, warehouseSettings, warehouseDataReader).ConfigureAwait(false);
        foreach (var pictureId in dto.PictureIds)
            await goodsReceipt.AddPictureAsync(pictureId, pictureDataReader).ConfigureAwait(false);

        if (dto.VendorId.HasValue)
        {
            var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is not null)
                goodsReceipt.SetVendor(vendor.Id, vendor.Name, vendor.PhoneNumber, vendor.Address);
        }

        goodsReceipt.MarkCreated();
        foreach (var itemId in goodsReceipt.Items.Where(i => i.UnitCost.HasValue).Select(i => i.Id))
            goodsReceipt.MarkItemUnitCostSet(itemId);

        var insertedGoodsReceipt = await goodsReceiptRepository.InsertAsync(goodsReceipt).ConfigureAwait(false);

        return new CreateGoodsReceiptResultDto { CreatedId = insertedGoodsReceipt.Id };
    }

    public async Task<UpdateGoodsReceiptResultDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.Id);

        goodsReceipt.SetReceivedDate(dto.ReceivedOnUtc);
        goodsReceipt.TruckDriverName = dto.TruckDriverName;
        goodsReceipt.TruckNumberSerial = dto.TruckNumberSerial;
        goodsReceipt.Note = dto.Note;
        goodsReceipt.ClearPictures();
        foreach (var pictureId in dto.PictureIds)
            await goodsReceipt.AddPictureAsync(pictureId, pictureDataReader).ConfigureAwait(false);

        if (dto.VendorId.HasValue)
        {
            var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is not null)
                goodsReceipt.SetVendor(vendor.Id, vendor.Name, vendor.PhoneNumber, vendor.Address);
        }
        else
        {
            goodsReceipt.ClearVendor();
        }

        goodsReceipt.MarkUpdated();
        var updatedGoodsReceipt = await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        return new UpdateGoodsReceiptResultDto { UpdatedId = updatedGoodsReceipt.Id };
    }

    public async Task SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.GoodsReceiptId);

        var item = goodsReceipt.Items.FirstOrDefault(item => item.Id == dto.GoodsReceiptItemId);
        if (item is null)
            throw new GoodsReceiptItemIsNotFoundException(dto.GoodsReceiptItemId);

        item.SetUnitCost(dto.UnitCost);
        goodsReceipt.MarkItemUnitCostSet(item.Id);
        await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);
    }

    public async Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(Guid id)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(id).ConfigureAwait(false);

        if (goodsReceipt is null)
            return null;
        return goodsReceipt.ToDto();
    }

    public async Task<IPagedDataDto<GoodsReceiptDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = goodsReceiptDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();

            IList<Guid?> userIds = [];

            query = query.Where(goodsReceipt =>
                (goodsReceipt.TruckDriverName.Value != null && (goodsReceipt.TruckDriverName.Value.ToUpper().Contains(uppercaseKeywords) || goodsReceipt.TruckDriverName.Value.ToUpper().Contains(normalizedKeywords) || goodsReceipt.TruckDriverName.NormalizedValue.Contains(normalizedKeywords)))
                || (goodsReceipt.TruckNumberSerial != null && goodsReceipt.TruckNumberSerial.ToUpper().Contains(uppercaseKeywords))
                || userIds.Contains(goodsReceipt.CreatedByUserId));
        }

        if (fromDateUtc.HasValue)
            query = query.Where(goodsReceipt => goodsReceipt.ReceivedOnUtc >= fromDateUtc);

        if (toDateUtc.HasValue)
            query = query.Where(goodsReceipt => goodsReceipt.ReceivedOnUtc <= toDateUtc);

        var total = query.Count();
        var data = query
            .OrderByDescending(o => o.CreatedOnUtc)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .ToList();

        var pagedData = PagedDataDto.Create(data.Select(goodsReceipt => goodsReceipt.ToDto()), pageIndex, pageSize, total);
        return pagedData;
    }

    public Task<IList<GoodsReceiptDto>> GetGoodsReceiptsByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        if (purchaseOrderId == Guid.Empty)
            return Task.FromResult<IList<GoodsReceiptDto>>([]);

        var data = goodsReceiptDataReader.DataSource
            .Where(gr => gr.PurchaseOrderId == purchaseOrderId)
            .OrderByDescending(gr => gr.ReceivedOnUtc)
            .ToList();

        IList<GoodsReceiptDto> result = data.Select(gr => gr.ToDto()).ToList();
        return Task.FromResult(result);
    }

    public async Task<SetGoodsReceiptVendorResultDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.GoodsReceiptId);

        if (dto.VendorId.HasValue)
        {
            var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is null)
                throw new VendorIsNotFoundException(dto.VendorId.Value);

            goodsReceipt.SetVendor(vendor.Id, vendor.Name, vendor.PhoneNumber, vendor.Address);
        }
        else
        {
            goodsReceipt.ClearVendor();
        }

        goodsReceipt.MarkVendorChanged();
        var updatedGoodsReceipt = await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        return new SetGoodsReceiptVendorResultDto { UpdatedId = updatedGoodsReceipt.Id };
    }

    public async Task DeleteGoodsReceiptAsync(DeleteGoodsReceiptDto dto)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.GoodsReceiptId);

        var linkedReturns = vendorReturnReader.DataSource
            .Where(r => r.GoodsReceiptId == dto.GoodsReceiptId)
            .Select(r => new { r.Id, r.Status })
            .ToList();

        if (linkedReturns.Any(r => r.Status == VendorReturnStatus.Confirmed))
            throw new GoodsReceiptHasConfirmedReturnsException(dto.GoodsReceiptId);

        var linkedDebt = vendorDebtReader.DataSource
            .FirstOrDefault(d => d.GoodsReceiptId == dto.GoodsReceiptId);
        if (linkedDebt is not null && (linkedDebt.PaidAmount > 0 || linkedDebt.RemainingAmount != linkedDebt.TotalAmount))
            throw new GoodsReceiptCannotDeleteDueToTouchedDebtException(dto.GoodsReceiptId);

        var cancellableReturnIds = linkedReturns
            .Where(r => r.Status == VendorReturnStatus.Draft || r.Status == VendorReturnStatus.Inspecting)
            .Select(r => r.Id)
            .ToList();

        foreach (var returnId in cancellableReturnIds)
            await vendorReturnManager.CancelAsync(returnId).ConfigureAwait(false);

        var deletedProductQuantities = goodsReceipt.Items
            .Where(item => item.WarehouseId.HasValue)
            .GroupBy(
                item => (item.ProductId, WarehouseId: item.WarehouseId!.Value),
                item => item.Quantity,
                (key, quantities) => (key.ProductId, key.WarehouseId, TotalQuantity: quantities.Sum())
        );
        foreach (var deletedProductQty in deletedProductQuantities)
        {
            var stock = (await inventoryStockManager.GetInventoryStocksForProductAsync(deletedProductQty.ProductId, deletedProductQty.WarehouseId).ConfigureAwait(false))
                .SingleOrDefault();
            var available = stock?.QuantityAvailable ?? 0;
            if (available < deletedProductQty.TotalQuantity)
                throw new GoodsReceiptCannotDeleteDueToReservedStockException(
                    dto.GoodsReceiptId,
                    deletedProductQty.ProductId,
                    deletedProductQty.WarehouseId,
                    deletedProductQty.TotalQuantity,
                    available);
        }

        if (linkedDebt is not null)
            await vendorDebtManager.DeleteDebtFromGoodsReceiptAsync(dto.GoodsReceiptId).ConfigureAwait(false);

        goodsReceipt.MarkDeleted();
        await goodsReceiptRepository.DeleteAsync(goodsReceipt).ConfigureAwait(false);
    }

    public async Task<CreateGoodsReceiptResultDto> CreateFromPurchaseOrderReceivingAsync(CreateGoodsReceiptFromPurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false)
            ?? throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        //*TODO*
        var createdByUser = purchaseOrder.CreatedByUserId.HasValue
                ? new CurrentUserInfoDto(purchaseOrder.CreatedByUserId.Value, string.Empty, string.Empty)
                : null;
        var goodsReceipt = await GoodsReceipt.CreateAsync(
            Guid.NewGuid(), createdByUser, goodsReceiptDataReader).ConfigureAwait(false);
        goodsReceipt.SetCode(await GenerateCodeAsync().ConfigureAwait(false));
        goodsReceipt.SetReceivedDate(DateTime.UtcNow);
        goodsReceipt.SetToPurchaseOrder(dto.PurchaseOrderId, dto.PurchaseOrderCode);

        // Gắn vendor từ PO (nếu có)
        if (dto.VendorId.HasValue)
        {
            var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is not null)
                goodsReceipt.SetVendor(vendor.Id, vendor.Name, vendor.PhoneNumber, vendor.Address);
        }

        // Thêm item — dùng WarehouseId từ dto (đã được PurchaseOrderManager resolve)
        await goodsReceipt.AddItemAsync(
            dto.ProductId, dto.WarehouseId, dto.Quantity, dto.UnitCost,
            productDataReader, warehouseSettings, warehouseDataReader
        ).ConfigureAwait(false);

        // MarkCreated → GoodsReceiptCreatedHandler: cộng tồn + thử sinh VendorDebt
        goodsReceipt.MarkCreated();

        // Nếu item đã có UnitCost, cũng fire GoodsReceiptItemUnitCostSet
        // → GoodsReceiptItemUnitCostSetHandler: cập nhật AverageCost + thử sinh VendorDebt (idempotent)
        if (dto.UnitCost.HasValue)
        {
            var addedItem = goodsReceipt.Items.Last();
            goodsReceipt.MarkItemUnitCostSet(addedItem.Id);
        }

        var insertedGoodsReceipt = await goodsReceiptRepository.InsertAsync(goodsReceipt).ConfigureAwait(false);
        return new CreateGoodsReceiptResultDto { CreatedId = insertedGoodsReceipt.Id };
    }

    public async Task<CreateGoodsReceiptResultDto> CreateBulkFromPurchaseOrderReceivingAsync(CreateGoodsReceiptFromPurchaseOrderBulkDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false)
            ?? throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        var createdByUser = purchaseOrder.CreatedByUserId.HasValue
                ? new CurrentUserInfoDto(purchaseOrder.CreatedByUserId.Value, string.Empty, string.Empty)
                : null;
        var goodsReceipt = await GoodsReceipt.CreateAsync(Guid.NewGuid(), createdByUser, goodsReceiptDataReader).ConfigureAwait(false);
        goodsReceipt.SetCode(await GenerateCodeAsync().ConfigureAwait(false));
        goodsReceipt.SetReceivedDate(DateTime.UtcNow);
        goodsReceipt.SetToPurchaseOrder(dto.PurchaseOrderId, dto.PurchaseOrderCode);

        if (dto.BulkReceiveBatchId.HasValue)
            goodsReceipt.SetBulkReceiveBatchId(dto.BulkReceiveBatchId.Value);

        if (dto.VendorId.HasValue)
        {
            var vendor = await vendorDataReader.GetByIdAsync(dto.VendorId.Value).ConfigureAwait(false);
            if (vendor is not null)
                goodsReceipt.SetVendor(vendor.Id, vendor.Name, vendor.PhoneNumber, vendor.Address);
        }

        // Track items đã có UnitCost để fire MarkItemUnitCostSet sau khi thêm xong.
        var itemsWithCost = new List<Guid>();
        foreach (var line in dto.Items)
        {
            await goodsReceipt.AddItemAsync(
                line.ProductId, dto.WarehouseId, line.Quantity, line.UnitCost,
                productDataReader, warehouseSettings, warehouseDataReader
            ).ConfigureAwait(false);

            if (line.UnitCost.HasValue)
                itemsWithCost.Add(goodsReceipt.Items.Last().Id);
        }

        // MarkCreated chạy 1 lần — handler cộng tồn cho TỪNG item + sinh VendorDebt cho cả phiếu.
        goodsReceipt.MarkCreated();

        // Sau khi insert, fire MarkItemUnitCostSet cho các item đã có cost → cập nhật AverageCost.
        foreach (var itemId in itemsWithCost)
            goodsReceipt.MarkItemUnitCostSet(itemId);

        var inserted = await goodsReceiptRepository.InsertAsync(goodsReceipt).ConfigureAwait(false);
        return new CreateGoodsReceiptResultDto { CreatedId = inserted.Id };
    }

    public async Task<Guid> CreateFromCustomerReturnAsync(CreateGoodsReceiptFromCustomerReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var customerReturn = await customerReturnReader.GetByIdAsync(dto.CustomerReturnId).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(dto.CustomerReturnId);

        var createdByUser = customerReturn.CreatedByUserId.HasValue
                ? new CurrentUserInfoDto(customerReturn.CreatedByUserId.Value, string.Empty, string.Empty)
                : null;
        var goodsReceipt = await GoodsReceipt.CreateAsync(Guid.NewGuid(), createdByUser, goodsReceiptDataReader).ConfigureAwait(false);
        goodsReceipt.SetCode(await GenerateCodeAsync().ConfigureAwait(false));
        goodsReceipt.SetReceivedDate(DateTime.UtcNow);
        goodsReceipt.SourceType = GoodsReceiptSourceType.FromCustomerReturn;

        foreach (var item in dto.Items)
        {
            await goodsReceipt.AddItemAsync(
                item.ProductId, dto.WarehouseId, item.Quantity, null,
                productDataReader, warehouseSettings, warehouseDataReader
            ).ConfigureAwait(false);
        }

        // MarkCreated → GoodsReceiptCreatedHandler sẽ cộng tồn kho.
        // Handler có guard: if SourceType == FromCustomerReturn → skip sinh VendorDebt.
        goodsReceipt.MarkCreated();

        var inserted = await goodsReceiptRepository.InsertAsync(goodsReceipt).ConfigureAwait(false);
        return inserted.Id;
    }

    public async Task<IList<SuggestedPurchaseOrderForGoodsReceiptDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(goodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(goodsReceiptId);

        var candidateOrders = purchaseOrderDataReader.DataSource
            .Where(po => po.PlacedOnUtc < goodsReceipt.ReceivedOnUtc
                && (goodsReceipt.VendorId == null || po.VendorId == goodsReceipt.VendorId)
                && (po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.Receiving))
            .ToList();

        if (candidateOrders.Count == 0)
            return [];

        var grGroups = goodsReceipt.Items
            .GroupBy(i => (i.ProductId, i.UnitCost))
            .Select(g => (Key: g.Key, ReceivingQty: g.Sum(i => i.Quantity)))
            .ToList();

        var totalGrQty = grGroups.Sum(g => g.ReceivingQty);
        if (totalGrQty == 0)
            return [];

        var results = new List<SuggestedPurchaseOrderForGoodsReceiptDto>();

        foreach (var po in candidateOrders)
        {
            var poRemainingMap = po.Items
                .GroupBy(i => (i.ProductId, i.UnitCost))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(i => i.QuantityOrdered - i.QuantityReceived));

            decimal matchedQty = 0;

            foreach (var grGroup in grGroups)
            {
                var (productId, unitCost) = grGroup.Key;
                var receivingQty = grGroup.ReceivingQty;

                if (unitCost.HasValue)
                {
                    if (poRemainingMap.TryGetValue((productId, unitCost.Value), out var exactRemaining))
                        matchedQty += Math.Min(exactRemaining, receivingQty);
                }
                else
                {
                    var remaining = receivingQty;
                    foreach (var poKey in poRemainingMap.Keys
                                 .Where(k => k.ProductId == productId && poRemainingMap[k] > 0)
                                 .ToList())
                    {
                        var canMatch = Math.Min(poRemainingMap[poKey], remaining);
                        matchedQty += canMatch;
                        remaining -= canMatch;
                        if (remaining == 0) break;
                    }
                }
            }

            var score = (int)Math.Round(matchedQty / totalGrQty * 100);
            score = Math.Clamp(score, 0, 100);

            var items = po.Items.Select(i => new SuggestedPurchaseOrderItemForGoodsReceiptDto
            {
                ProductId = i.ProductId,
                QuantityOrdered = i.QuantityOrdered,
                QuantityReceived = i.QuantityReceived,
                UnitCost = i.UnitCost
            }).ToList();

            results.Add(new SuggestedPurchaseOrderForGoodsReceiptDto
            {
                PurchaseOrderId = po.Id,
                PurchaseOrderCode = po.Code,
                PlacedOnUtc = po.PlacedOnUtc,
                VendorId = po.VendorId,
                MatchScore = score,
                IsFullMatch = score == 100,
                Items = items
            });
        }

        return results
            .OrderByDescending(r => r.IsFullMatch)
            .ThenByDescending(r => r.MatchScore)
            .ThenByDescending(r => r.PlacedOnUtc)
            .Take(20)
            .ToList();
    }

}
