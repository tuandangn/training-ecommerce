using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Inventory;
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
    IRepository<PurchaseOrder> purchaseOrderRepository,
    IInventoryStockManager inventoryStockManager) : IGoodsReceiptManager
{
    public async Task<CreateGoodsReceiptResultDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await GoodsReceipt.CreateAsync(Guid.NewGuid(), goodsReceiptDataReader, currentUserAccessor);
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
        var updatedGoodsReceipt = await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        // Pass item.Id qua AdditionalData để GoodsReceiptUpdatedHandler phân biệt được
        // đây là một lần SetUnitCost (cần Full Recalculation AverageCost) thay vì các loại update khác.
        await eventPublisher.EntityUpdated(updatedGoodsReceipt, item.Id).ConfigureAwait(false);
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
                (goodsReceipt.TruckDriverName != null && (goodsReceipt.TruckDriverName.ToUpper().Contains(uppercaseKeywords) || goodsReceipt.TruckDriverName.ToUpper().Contains(normalizedKeywords) || goodsReceipt.TruckDriverNameNormalized.Contains(normalizedKeywords)))
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

        var updatedGoodsReceipt = await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedGoodsReceipt, "vendor-updated").ConfigureAwait(false);

        return new SetGoodsReceiptVendorResultDto { UpdatedId = updatedGoodsReceipt.Id };
    }

    public async Task DeleteGoodsReceiptAsync(DeleteGoodsReceiptDto dto)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.GoodsReceiptId);

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
            if (stock is null || stock.QuantityAvailable < deletedProductQty.TotalQuantity)
                throw new InsufficientStockException(deletedProductQty.ProductId, deletedProductQty.WarehouseId, deletedProductQty.TotalQuantity, 0);
        }

        await goodsReceiptRepository.DeleteAsync(goodsReceipt).ConfigureAwait(false);

        await eventPublisher.EntityDeleted(goodsReceipt).ConfigureAwait(false);
    }

    public Task RemoveGoodsReceiptFromPurchaseOrder(RemoveGoodsReceiptFromPurchaseOrderDto dto)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<SuggestedPurchaseOrderForGoodsReceiptDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(goodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(goodsReceiptId);

        // Chỉ lấy PO đặt trước ngày nhận hàng và đang ở trạng thái có thể nhận hàng.
        var candidateOrders = purchaseOrderDataReader.DataSource
            .Where(po => po.PlacedOnUtc < goodsReceipt.ReceivedOnUtc
                      && (po.Status == PurchaseOrderStatus.Approved || po.Status == PurchaseOrderStatus.Receiving))
            .ToList();

        if (candidateOrders.Count == 0)
            return [];

        // GR items gộp theo (ProductId, UnitCost) — UnitCost null = chưa định giá (pending costing).
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
            // PO items gộp theo (ProductId, UnitCost) — tính số lượng còn lại chưa nhận.
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
                    // Exact match: cùng ProductId và cùng UnitCost.
                    if (poRemainingMap.TryGetValue((productId, unitCost.Value), out var exactRemaining))
                        matchedQty += Math.Min(exactRemaining, receivingQty);
                }
                else
                {
                    // Partial match: cùng ProductId, bất kỳ UnitCost — duyệt theo thứ tự để consume hết remaining.
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

    #region SetGoodsReceiptToPurchaseOrder

    public async Task SetGoodsReceiptToPurchaseOrder(SetGoodsReceiptToPurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.Id);

        if (goodsReceipt.PurchaseOrderId.HasValue)
            throw new GoodsReceiptCannotSetToPurchaseOrderException();

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var grProductCostReceivingQtyMap = goodsReceipt.Items
            .GroupBy(
                item => (item.ProductId, item.UnitCost),
                item => (receivingQuantity: item.Quantity, item.Id),
                (key, infos) => new _GoodsReceiptProductCostReceivingQtyItem(
                    key.ProductId, key.UnitCost,
                    infos.Select(info => new _GoodsReceiptReceivedItemInfo(info.Id)
                    {
                        ReceivedQuantity = info.receivingQuantity
                    }).ToList())
                {
                    TotalReceivedQuantity = infos.Sum(info => info.receivingQuantity)
                }
            ).ToList();
        var poProductCostRemainingQtyMap = purchaseOrder.Items
            .Where(item => item.QuantityOrdered > item.QuantityReceived)
            .GroupBy(
                item => (item.ProductId, item.UnitCost),
                item => (remainQuantity: item.QuantityOrdered - item.QuantityReceived, item.Id),
                (key, infos) => new _PurchaseOrderProductCostRemainingQtyItem(key.ProductId, key.UnitCost, infos.Select(info => new _PurchaseOrderItemInfo(info.Id) { RemainingQuantity = info.remainQuantity }).ToList())
                {
                    TotalRemainingQuantity = infos.Sum(info => info.remainQuantity)
                }
            ).ToList();

        var resolvePoItems = new List<(Guid itemId, decimal qty)>();
        var needUpdateUnitCostItems = new List<(Guid itemId, decimal qty, decimal unitCost)>();

        foreach (var grItem in grProductCostReceivingQtyMap.Where(gr => gr.UnitCost.HasValue))
        {
            var poItem = poProductCostRemainingQtyMap.FirstOrDefault(po =>
                po.ProductId == grItem.ProductId && po.UnitCost == grItem.UnitCost);

            if (poItem == null || poItem.TotalRemainingQuantity < grItem.TotalReceivedQuantity)
                throw new GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(grItem.ProductId, grItem.TotalReceivedQuantity);

            foreach (var participant in poItem.Participants.Where(p => p.RemainingQuantity > 0))
            {
                if (grItem.TotalReceivedQuantity <= 0)
                    break;

                var resolvedQty = Math.Min(participant.RemainingQuantity, grItem.TotalReceivedQuantity);

                participant.RemainingQuantity -= resolvedQty;
                poItem.TotalRemainingQuantity -= resolvedQty;
                grItem.TotalReceivedQuantity -= resolvedQty;

                resolvePoItems.Add((participant.ItemId, resolvedQty));
            }
        }

        foreach (var grItem in grProductCostReceivingQtyMap.Where(gr => !gr.UnitCost.HasValue))
        {
            foreach (var receivedItem in grItem.ReceivedItems.Where(ri => ri.ReceivedQuantity > 0))
            {
                var compatiblePos = poProductCostRemainingQtyMap.Where(po => po.ProductId == grItem.ProductId && po.TotalRemainingQuantity > 0);

                foreach (var poItem in compatiblePos)
                {
                    if (receivedItem.ReceivedQuantity <= 0)
                        break;

                    foreach (var participant in poItem.Participants.Where(p => p.RemainingQuantity > 0))
                    {
                        if (receivedItem.ReceivedQuantity <= 0) break;

                        var resolvedQty = Math.Min(participant.RemainingQuantity, receivedItem.ReceivedQuantity);

                        receivedItem.ReceivedQuantity -= resolvedQty;
                        participant.RemainingQuantity -= resolvedQty;
                        poItem.TotalRemainingQuantity -= resolvedQty;
                        grItem.TotalReceivedQuantity -= resolvedQty;

                        needUpdateUnitCostItems.Add((receivedItem.ItemId, resolvedQty, poItem.UnitCost));
                        resolvePoItems.Add((participant.ItemId, resolvedQty));
                    }
                }
            }

            if (grItem.TotalReceivedQuantity > 0)
                throw new GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(grItem.ProductId, grItem.TotalReceivedQuantity);
        }
        foreach (var group in needUpdateUnitCostItems.GroupBy(i => i.itemId))
        {
            var goodsReceiptItem = goodsReceipt.Items.First(item => item.Id == group.Key);
            for (var i = 0; i < group.Count(); i++)
            {
                var (itemId, qty, unitCost) = group.ElementAt(i);
                if (i == group.Count() - 1)
                    goodsReceiptItem.SetUnitCost(unitCost);
                else
                {
                    goodsReceipt.SplitToNewItemWithQuantity(itemId, qty);
                    var newItem = goodsReceipt.Items.Last();
                    newItem.SetUnitCost(unitCost);
                }
            }
        }
        goodsReceipt.SetToPurchaseOrder(purchaseOrder.Id, purchaseOrder.Code);
        await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        foreach (var (itemId, qty) in resolvePoItems)
        {
            var purchaseOrderItem = purchaseOrder.Items.First(item => item.Id == itemId);
            purchaseOrderItem.AddQuantityReceived(qty);
        }
        purchaseOrder.VerifyStatus();
        purchaseOrder.UpdatedOnUtc = DateTime.UtcNow;
        await purchaseOrderRepository.UpdateAsync(purchaseOrder).ConfigureAwait(false);
    }

    #region Helper classes

    private record _GoodsReceiptProductCostReceivingQtyItem(Guid ProductId, decimal? UnitCost, IList<_GoodsReceiptReceivedItemInfo> ReceivedItems)
    {
        public decimal TotalReceivedQuantity { get; set; }
    };
    private record _GoodsReceiptReceivedItemInfo(Guid ItemId)
    {
        public decimal ReceivedQuantity { get; set; }
    };
    private record _PurchaseOrderProductCostRemainingQtyItem(Guid ProductId, decimal UnitCost, IList<_PurchaseOrderItemInfo> Participants)
    {
        public decimal TotalRemainingQuantity { get; set; }
    };
    private record _PurchaseOrderItemInfo(Guid ItemId)
    {
        public decimal RemainingQuantity { get; set; }
    };

    #endregion

    #endregion
}
