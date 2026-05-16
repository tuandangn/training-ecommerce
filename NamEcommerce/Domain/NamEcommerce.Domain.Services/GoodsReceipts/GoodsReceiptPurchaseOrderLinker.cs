using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.PurchaseOrders;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Domain.Services.GoodsReceipts;

public sealed class GoodsReceiptPurchaseOrderLinker(
    IRepository<GoodsReceipt> goodsReceiptRepository,
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    IRepository<PurchaseOrder> purchaseOrderRepository,
    IEntityDataReader<PurchaseOrder> purchaseOrderDataReader,
    IPurchaseOrderAllocationManager purchaseOrderAllocationManager) : IGoodsReceiptPurchaseOrderLinker
{
    public async Task LinkAsync(SetGoodsReceiptToPurchaseOrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new GoodsReceiptIsNotFoundException(dto.Id);

        if (goodsReceipt.PurchaseOrderId.HasValue)
            throw new GoodsReceiptCannotSetToPurchaseOrderException();

        var purchaseOrder = await purchaseOrderDataReader.GetByIdAsync(dto.PurchaseOrderId).ConfigureAwait(false)
            ?? throw new PurchaseOrderIsNotFoundException(dto.PurchaseOrderId);

        if (!purchaseOrder.CanReceiveGoods())
            throw new PurchaseOrderCannotReceiveGoodsException();

        var grBuckets = BuildGoodsReceiptBuckets(goodsReceipt);
        var poBuckets = BuildPurchaseOrderBuckets(purchaseOrder);

        var resolvePoItems = new List<(Guid itemId, decimal qty)>();
        var needUpdateUnitCostItems = new List<(Guid grItemId, decimal qty, decimal unitCost)>();

        // Pass 1: GR items đã có UnitCost → match thẳng theo (ProductId, UnitCost).
        ResolveCostedItems(grBuckets, poBuckets, resolvePoItems);

        // Pass 2: GR items chưa có UnitCost → match theo ProductId, có thể split khi PO có nhiều cost khác.
        ResolveUncostedItems(grBuckets, poBuckets, resolvePoItems, needUpdateUnitCostItems);

        ApplyCostAssignmentsAndSplit(goodsReceipt, purchaseOrder.Id, needUpdateUnitCostItems);

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

        foreach (var itemId in resolvePoItems.Select(item => item.itemId).Distinct())
        {
            var purchaseOrderItem = purchaseOrder.Items.First(item => item.Id == itemId);
            await purchaseOrderAllocationManager
                .SyncReceivedForPurchaseOrderItemAsync(itemId, purchaseOrderItem.QuantityReceived)
                .ConfigureAwait(false);
        }
    }

    private static List<GoodsReceiptBucket> BuildGoodsReceiptBuckets(GoodsReceipt goodsReceipt)
        => goodsReceipt.Items
            .GroupBy(
                item => (item.ProductId, item.UnitCost),
                item => (receivingQuantity: item.Quantity, item.Id),
                (key, infos) => new GoodsReceiptBucket(
                    key.ProductId, key.UnitCost,
                    infos.Select(info => new GoodsReceiptReceivedItem(info.Id) { ReceivedQuantity = info.receivingQuantity }).ToList())
                {
                    TotalReceivedQuantity = infos.Sum(info => info.receivingQuantity)
                })
            .ToList();

    private static List<PurchaseOrderBucket> BuildPurchaseOrderBuckets(PurchaseOrder purchaseOrder)
        => purchaseOrder.Items
            .Where(item => item.QuantityOrdered > item.QuantityReceived)
            .GroupBy(
                item => (item.ProductId, item.UnitCost),
                item => (remainQuantity: item.QuantityOrdered - item.QuantityReceived, item.Id),
                (key, infos) => new PurchaseOrderBucket(key.ProductId, key.UnitCost,
                    infos.Select(info => new PurchaseOrderItemRemainder(info.Id) { RemainingQuantity = info.remainQuantity }).ToList())
                {
                    TotalRemainingQuantity = infos.Sum(info => info.remainQuantity)
                })
            .ToList();

    private static void ResolveCostedItems(
        List<GoodsReceiptBucket> grBuckets,
        List<PurchaseOrderBucket> poBuckets,
        List<(Guid itemId, decimal qty)> resolvePoItems)
    {
        foreach (var grBucket in grBuckets.Where(gr => gr.UnitCost.HasValue))
        {
            var poBucket = poBuckets.FirstOrDefault(po =>
                po.ProductId == grBucket.ProductId && po.UnitCost == grBucket.UnitCost);

            if (poBucket is null || poBucket.TotalRemainingQuantity < grBucket.TotalReceivedQuantity)
                throw new GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(grBucket.ProductId, grBucket.TotalReceivedQuantity);

            foreach (var participant in poBucket.Participants.Where(p => p.RemainingQuantity > 0))
            {
                if (grBucket.TotalReceivedQuantity <= 0) break;

                var resolvedQty = Math.Min(participant.RemainingQuantity, grBucket.TotalReceivedQuantity);

                participant.RemainingQuantity -= resolvedQty;
                poBucket.TotalRemainingQuantity -= resolvedQty;
                grBucket.TotalReceivedQuantity -= resolvedQty;

                resolvePoItems.Add((participant.ItemId, resolvedQty));
            }
        }
    }

    private static void ResolveUncostedItems(
        List<GoodsReceiptBucket> grBuckets,
        List<PurchaseOrderBucket> poBuckets,
        List<(Guid itemId, decimal qty)> resolvePoItems,
        List<(Guid grItemId, decimal qty, decimal unitCost)> needUpdateUnitCostItems)
    {
        foreach (var grBucket in grBuckets.Where(gr => !gr.UnitCost.HasValue))
        {
            foreach (var receivedItem in grBucket.ReceivedItems.Where(ri => ri.ReceivedQuantity > 0))
            {
                var compatiblePos = poBuckets.Where(po => po.ProductId == grBucket.ProductId && po.TotalRemainingQuantity > 0);

                foreach (var poBucket in compatiblePos)
                {
                    if (receivedItem.ReceivedQuantity <= 0) break;

                    foreach (var participant in poBucket.Participants.Where(p => p.RemainingQuantity > 0))
                    {
                        if (receivedItem.ReceivedQuantity <= 0) break;

                        var resolvedQty = Math.Min(participant.RemainingQuantity, receivedItem.ReceivedQuantity);

                        receivedItem.ReceivedQuantity -= resolvedQty;
                        participant.RemainingQuantity -= resolvedQty;
                        poBucket.TotalRemainingQuantity -= resolvedQty;
                        grBucket.TotalReceivedQuantity -= resolvedQty;

                        needUpdateUnitCostItems.Add((receivedItem.ItemId, resolvedQty, poBucket.UnitCost));
                        resolvePoItems.Add((participant.ItemId, resolvedQty));
                    }
                }
            }

            if (grBucket.TotalReceivedQuantity > 0)
                throw new GoodsReceiptItemCannotResolvedWhenSetToPurchaseOrderException(grBucket.ProductId, grBucket.TotalReceivedQuantity);
        }
    }

    private static void ApplyCostAssignmentsAndSplit(
        GoodsReceipt goodsReceipt,
        Guid purchaseOrderId,
        List<(Guid grItemId, decimal qty, decimal unitCost)> assignments)
    {
        foreach (var group in assignments.GroupBy(i => i.grItemId))
        {
            var assignmentList = group.ToList();
            var goodsReceiptItem = goodsReceipt.Items.First(item => item.Id == group.Key);

            for (var i = 0; i < assignmentList.Count; i++)
            {
                var (originalItemId, qty, unitCost) = assignmentList[i];
                var isLast = i == assignmentList.Count - 1;
                if (isLast)
                {
                    goodsReceiptItem.SetUnitCost(unitCost);
                }
                else
                {
                    goodsReceipt.SplitToNewItemWithQuantity(originalItemId, qty);
                    var newItem = goodsReceipt.Items.Last();
                    newItem.SetUnitCost(unitCost);
                    goodsReceipt.RaiseItemSplitOnLinking(
                        purchaseOrderId, originalItemId, newItem.ProductId, qty, unitCost);
                }
            }
        }
    }

    private sealed record GoodsReceiptBucket(Guid ProductId, decimal? UnitCost, IList<GoodsReceiptReceivedItem> ReceivedItems)
    {
        public decimal TotalReceivedQuantity { get; set; }
    }

    private sealed record GoodsReceiptReceivedItem(Guid ItemId)
    {
        public decimal ReceivedQuantity { get; set; }
    }

    private sealed record PurchaseOrderBucket(Guid ProductId, decimal UnitCost, IList<PurchaseOrderItemRemainder> Participants)
    {
        public decimal TotalRemainingQuantity { get; set; }
    }

    private sealed record PurchaseOrderItemRemainder(Guid ItemId)
    {
        public decimal RemainingQuantity { get; set; }
    }
}
