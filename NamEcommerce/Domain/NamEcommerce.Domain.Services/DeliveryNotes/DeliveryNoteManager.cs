using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Domain.Services.DeliveryNotes;

public sealed class DeliveryNoteManager(
    IRepository<DeliveryNote> deliveryNoteRepository,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IOrderManager orderManager,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager,
    IProductReservationManager productReservationManager,
    IEntityDataReader<CustomerReturn> customerReturnReader,
    IEntityDataReader<VendorReturn> vendorReturnReader,
    ICustomerReturnManager customerReturnManager) : IDeliveryNoteManager
{
    private Task<string> GenerateCodeAsync()
    {
        var monthPrefix = $"{DeliveryNote.CODE_PREFIX}-{DateTime.UtcNow:yyMM}";
        var count = deliveryNoteReader.SecuredDataSource.Count(d => d.Code.StartsWith(monthPrefix));
        return Task.FromResult($"{monthPrefix}-{(count + 1):D3}");
    }

    private async Task<decimal> GetDisplayCostAsync(Guid productId)
    {
        var summary = await inventoryCostingManager.GetCurrentCostSummaryAsync(productId).ConfigureAwait(false);
        return summary.AverageCost;
    }

    public async Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto)
    {
        dto.Verify();

        var order = await orderReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var orderItemsById = order.OrderItems.ToDictionary(item => item.Id);
        var requestedQuantitiesByOrderItem = dto.Items
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity));

        EnsureQuantitiesCanBeDelivered(order, requestedQuantitiesByOrderItem, dto.CompensateReturnedQuantityInNextDelivery);

        var itemsByProduct = dto.Items
            .GroupBy(item => orderItemsById[item.OrderItemId].ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(item => item.Quantity) })
            .ToList();

        foreach (var item in itemsByProduct)
        {
            var reservedQuantity = await productReservationManager.GetReservedForOrderAsync(item.ProductId, order.Id).ConfigureAwait(false);
            if (!dto.CompensateReturnedQuantityInNextDelivery && reservedQuantity < item.Quantity)
                throw new InvalidStockOperationException("Error.CannotReleaseMoreThanReserved", reservedQuantity, item.Quantity);
        }

        foreach (var item in itemsByProduct)
        {
            var stock = await stockManager.GetInventoryStockForProductAsync(item.ProductId, dto.WarehouseId).ConfigureAwait(false);
            if (stock is null)
                throw new InsufficientStockException(item.ProductId, dto.WarehouseId, item.Quantity, 0);
            if (stock.QuantityAvailable < item.Quantity)
                throw new InsufficientStockException(item.ProductId, dto.WarehouseId, item.Quantity, stock.QuantityAvailable);
        }

        var code = await GenerateCodeAsync().ConfigureAwait(false);

        var deliveryNote = new DeliveryNote(
            code: code,
            orderId: order.Id,
            customerId: order.CustomerId,
            customerName: order.CustomerInfo.FullName,
            customerPhone: order.CustomerInfo.PhoneNumber,
            customerAddress: order.CustomerInfo.Address,
            shippingAddress: dto.ShippingAddress,
            warehouseId: dto.WarehouseId,
            showPrice: dto.ShowPrice,
            note: dto.Note,
            surcharge: dto.Surcharge,
            amountToCollect: dto.AmountToCollect,
            surchargeReason: dto.SurchargeReason,
            createdByUserId: order.CreatedByUserId
        )
        {
            OrderCode = order.Code,
            WarehouseName = dto.WarehouseName
        };

        foreach (var itemDto in dto.Items)
        {
            var orderItem = orderItemsById[itemDto.OrderItemId];

            deliveryNote.AddItem(
                orderItemId: orderItem.Id,
                productId: orderItem.ProductId,
                productName: orderItem.ProductName ?? string.Empty,
                quantity: itemDto.Quantity,
                unitPrice: orderItem.UnitPrice
            );
        }

        deliveryNote.MarkCreated();
        var inserted = await deliveryNoteRepository.InsertAsync(deliveryNote).ConfigureAwait(false);

        return MapToDto(inserted);
    }

    public async Task ConfirmAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        if (deliveryNote.Status != DeliveryNoteStatus.Draft)
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, DeliveryNoteStatus.Confirmed);

        foreach (var item in deliveryNote.Items)
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);

        deliveryNote.Confirm();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task MarkDeliveringAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        deliveryNote.MarkDelivering();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        var acceptance = ResolveDeliveryAcceptance(deliveryNote, dto.Acceptance);

        // Snapshot hiển thị trước khi xuất kho; COGS authoritative được ghi trong cost allocation.
        // Lưu cùng entity — DeliveryNoteDeliveredStockHandler sẽ dispatch stock sau khi event fire.
        foreach (var item in deliveryNote.Items)
        {
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
        }

        deliveryNote.AmountToCollect = acceptance.AmountToCollect;

        // 1. Mark DeliveryNote Delivered — raise DeliveryNoteDelivered event
        deliveryNote.MarkDelivered(dto.PictureId, dto.ReceiverName);

        // Save entity (display cost + status) → interceptor fires event → DeliveryNoteDeliveredStockHandler dispatches stock/cost.
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        // 2. Nếu khách không nhận đủ hàng, tạo phiếu CustomerReturn draft tự động để vào bước kiểm hàng/xác nhận.
        await CreateCustomerReturnFromRejectedAcceptanceAsync(deliveryNote, acceptance).ConfigureAwait(false);

        // 2. Mark related OrderItems as Delivered only when the full ordered quantity has been delivered.
        var order = await orderReader.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
        if (order is not null)
        {
            var deliveredQuantitiesByOrderItem = GetNetDeliveredQuantitiesByOrderItem(order.Id);

            foreach (var noteItem in deliveryNote.Items.Where(item => item.OrderItemId != Guid.Empty))
            {
                var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == noteItem.OrderItemId);
                var deliveredQuantity = deliveredQuantitiesByOrderItem.GetValueOrDefault(noteItem.OrderItemId);
                if (orderItem != null && !orderItem.IsDelivered && deliveredQuantity >= orderItem.Quantity)
                {
                    await orderManager.MarkOrderItemDeliveredAsync(new MarkOrderItemDeliveredDto
                    {
                        OrderId = order.Id,
                        OrderItemId = orderItem.Id,
                        PictureId = dto.PictureId
                    }).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task MarkReceivedByCustomerAsync(Guid id, DateTime receivedAtUtc, string? receiverName,
        string? note, DeliveryAcceptanceDto? acceptance = null)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        DeliveryAcceptanceResolution? resolvedAcceptance = null;
        if (deliveryNote.Status != DeliveryNoteStatus.Delivered)
        {
            resolvedAcceptance = ResolveDeliveryAcceptance(deliveryNote, acceptance);
            foreach (var item in deliveryNote.Items)
            {
                item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
            }

            deliveryNote.AmountToCollect = resolvedAcceptance.AmountToCollect;
        }

        var transitionedToDelivered = deliveryNote.MarkReceivedByCustomer(receivedAtUtc, receiverName, note);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        if (transitionedToDelivered && resolvedAcceptance is not null)
            await CreateCustomerReturnFromRejectedAcceptanceAsync(deliveryNote, resolvedAcceptance).ConfigureAwait(false);

        if (transitionedToDelivered)
            await MarkRelatedOrderItemsReceivedByCustomerAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task<Guid> CreateAsDeliveredAsync(CreateDeliveryNoteFromVendorReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var vendorReturn = await vendorReturnReader.GetByIdAsync(dto.VendorReturnId).ConfigureAwait(false)
            ?? throw new VendorReturnNotFoundException(dto.VendorReturnId);

        var code = await GenerateCodeAsync().ConfigureAwait(false);

        var deliveryNote = new DeliveryNote(
            code: code,
            warehouseId: dto.WarehouseId,
            note: null,
            createdByUserId: vendorReturn.CreatedByUserId);

        deliveryNote.SourceType = DeliveryNoteSourceType.ToVendorReturn;

        foreach (var item in dto.Items)
            deliveryNote.AddItemFromVendorReturn(item.ProductId, item.ProductName, item.Quantity, item.UnitCost);

        // Snapshot hiển thị trước khi xuất kho; COGS authoritative được ghi trong cost allocation.
        foreach (var item in deliveryNote.Items)
        {
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
        }

        // Chuyển thẳng sang Delivered và raise DeliveryNoteDelivered event.
        // DeliveryNoteDeliveredStockHandler sẽ dispatch stock; DeliveryNoteDeliveredEventHandler có guard
        // SourceType == ToVendorReturn → skip sinh CustomerDebt.
        deliveryNote.MarkAsDeliveredFromVendorReturn();

        var inserted = await deliveryNoteRepository.InsertAsync(deliveryNote).ConfigureAwait(false);

        return inserted.Id;
    }

    public async Task<Guid> CreateForDirectShipAsync(CreateDeliveryNoteForDirectShipDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = orderReader.DataSource
            .FirstOrDefault(o => o.OrderItems.Any(oi => oi.Id == dto.OrderItemId))
            ?? throw new OrderIsNotFoundException(dto.OrderItemId);

        var orderItem = order.OrderItems.First(oi => oi.Id == dto.OrderItemId);
        EnsureQuantitiesCanBeDelivered(order, new Dictionary<Guid, decimal>
        {
            [orderItem.Id] = dto.Quantity
        }, includeReturnedCompensation: false);

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        string contactName = string.IsNullOrWhiteSpace(dto.ContactName)
            ? order.CustomerInfo.FullName
            : dto.ContactName.Trim();
        string contactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone)
            ? order.CustomerInfo.PhoneNumber
            : dto.ContactPhone.Trim();

        var deliveryNote = new DeliveryNote(
            code: code,
            orderId: order.Id,
            customerId: order.CustomerId,
            customerName: contactName,
            customerPhone: contactPhone,
            customerAddress: dto.ShippingAddress,
            shippingAddress: dto.ShippingAddress,
            warehouseId: dto.DirectShipWarehouseId,
            showPrice: false,
            note: null,
            surcharge: 0,
            amountToCollect: 0,
            surchargeReason: null,
            createdByUserId: null)
        {
            OrderCode = order.Code
        };

        deliveryNote.AddItem(
            orderItemId: orderItem.Id,
            productId: orderItem.ProductId,
            productName: orderItem.ProductName ?? string.Empty,
            quantity: dto.Quantity,
            unitPrice: orderItem.UnitPrice);

        deliveryNote.SetAsDirectShip(dto.GoodsReceiptId);
        deliveryNote.MarkCreated();
        deliveryNote.Confirm();

        var inserted = await deliveryNoteRepository.InsertAsync(deliveryNote, ct).ConfigureAwait(false);

        return inserted.Id;
    }

    public async Task ConfirmDirectShipDeliveryAsync(Guid id, DateTime confirmedAtUtc, string? note, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        foreach (var item in deliveryNote.Items)
        {
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
        }

        deliveryNote.ConfirmDirectShipDelivery(confirmedAtUtc, note);
        await deliveryNoteRepository.UpdateAsync(deliveryNote, ct).ConfigureAwait(false);
    }

    public async Task RejectDirectShipDeliveryAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        deliveryNote.RejectDirectShipDelivery(reason);
        await deliveryNoteRepository.UpdateAsync(deliveryNote, ct).ConfigureAwait(false);
        foreach (var item in deliveryNote.Items)
        {
            await productReservationManager.ReserveAsync(item.ProductId, item.Quantity, deliveryNote.OrderId, ProductReservationReason.DeliveryNoteCancelled, deliveryNote.Id).ConfigureAwait(false);
        }
    }

    public async Task CancelAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        var linkedReturns = customerReturnReader.DataSource
            .Where(r => r.DeliveryNoteId == id)
            .Select(r => new { r.Id, r.Status })
            .ToList();

        if (linkedReturns.Any(r => r.Status == CustomerReturnStatus.Confirmed))
            throw new DeliveryNoteHasConfirmedReturnsException(id);

        var cancellableReturnIds = linkedReturns
            .Where(r => r.Status == CustomerReturnStatus.Draft || r.Status == CustomerReturnStatus.Inspecting)
            .Select(r => r.Id)
            .ToList();

        foreach (var returnId in cancellableReturnIds)
            await customerReturnManager.CancelAsync(returnId).ConfigureAwait(false);

        bool wasDraft = deliveryNote.Status == DeliveryNoteStatus.Draft;
        bool wasConfirmed = deliveryNote.Status == DeliveryNoteStatus.Confirmed || deliveryNote.Status == DeliveryNoteStatus.Delivering;

        deliveryNote.Cancel();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        if (wasDraft && deliveryNote.SourceType == DeliveryNoteSourceType.ToCustomer && !deliveryNote.IsDirectShip)
        {
            var itemsByProduct = deliveryNote.Items
                .GroupBy(item => item.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(item => item.Quantity) })
                .ToList();

            foreach (var item in itemsByProduct)
            {
                await stockManager.ReleaseReservedStockAsync(
                    item.ProductId,
                    deliveryNote.WarehouseId,
                    item.Quantity,
                    deliveryNote.Id,
                    Guid.Empty,
                    $"Giải phóng hàng phiếu xuất {deliveryNote.Code} bị hủy").ConfigureAwait(false);
            }

            if (deliveryNote.OrderId != Guid.Empty)
            {
                foreach (var item in itemsByProduct)
                {
                    var releasedQuantity = await productReservationManager.GetReleasedByReferenceAsync(
                        item.ProductId,
                        deliveryNote.OrderId,
                        ProductReservationReason.DeliveryNoteCreated,
                        deliveryNote.Id).ConfigureAwait(false);
                    var quantityToReserve = Math.Min(item.Quantity, releasedQuantity);
                    if (quantityToReserve <= 0)
                        continue;

                    await productReservationManager.ReserveAsync(
                        item.ProductId,
                        quantityToReserve,
                        deliveryNote.OrderId,
                        ProductReservationReason.DeliveryNoteCancelled,
                        deliveryNote.Id).ConfigureAwait(false);
                }
            }
        }
        else if (wasConfirmed && deliveryNote.SourceType == DeliveryNoteSourceType.ToCustomer && !deliveryNote.IsDirectShip)
        {
            foreach (var item in deliveryNote.Items)
            {
                await stockManager.ReceiveStockUpToAsync(
                    item.ProductId,
                    deliveryNote.WarehouseId,
                    deliveryNote.Items
                        .Where(i => i.ProductId == item.ProductId)
                        .Sum(i => i.Quantity),
                    $"Nhập lại kho do hủy phiếu xuất {deliveryNote.Code}",
                    Guid.Empty,
                    (int)StockReferenceType.SalesOrder,
                    deliveryNote.Id).ConfigureAwait(false);

                await inventoryCostingManager.RegisterInboundAsync(new RegisterInventoryInboundCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = deliveryNote.WarehouseId,
                    Quantity = item.Quantity,
                    UnitCost = item.CostAtDispatch,
                    MovementType = InventoryCostMovementType.CustomerReturn,
                    ReferenceType = InventoryCostReferenceType.SalesOrder,
                    ReferenceId = deliveryNote.Id,
                    ReferenceItemId = item.Id,
                    OccurredAtUtc = DateTime.UtcNow
                }).ConfigureAwait(false);
            }

            if (deliveryNote.OrderId != Guid.Empty)
            {
                var itemsByProduct = deliveryNote.Items
                    .GroupBy(item => item.ProductId)
                    .Select(g => new { ProductId = g.Key, Quantity = g.Sum(item => item.Quantity) })
                    .ToList();

                foreach (var item in itemsByProduct)
                {
                    await productReservationManager.ReserveAsync(
                        item.ProductId,
                        item.Quantity,
                        deliveryNote.OrderId,
                        ProductReservationReason.DeliveryNoteCancelled,
                        deliveryNote.Id).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task<DeliveryNoteDto?> GetByIdAsync(Guid id)
    {
        var baseQuery = deliveryNoteReader.DataSource; // Eager loading should happen here if possible, but IEntityDataReader usually handles it
        // Or we map manually. Assuming GetByIdAsync works normally but doesn't eager load Items if not configured.
        // Actually, we should just query and map.
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(id).ConfigureAwait(false);
        return deliveryNote is null ? null : MapToDto(deliveryNote);
    }

    public async Task<IPagedDataDto<DeliveryNoteDto>> GetDeliveryNotesAsync(int pageIndex, int pageSize, 
        string? keywords, Guid? orderId, IEnumerable<DeliveryNoteStatus>? status)
    {
        var query = deliveryNoteReader.DataSource;

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var uppercaseKeywords = keywords.Trim().ToUpper();
            query = query.Where(deliveryNote =>
                deliveryNote.Code.Contains(keywords) ||
                (deliveryNote.OrderCode != null && deliveryNote.OrderCode.Contains(keywords)) ||
                deliveryNote.CustomerInfo.FullName.Value.ToUpper().Contains(uppercaseKeywords) ||
                (deliveryNote.CustomerInfo.PhoneNumber != null && deliveryNote.CustomerInfo.PhoneNumber.Contains(keywords))
            );
        }
        if (orderId.HasValue)
            query = query.Where(deliverNote => deliverNote.OrderId == orderId);
        if (status != null && status.Any())
            query = query.Where(deliverNote => status.Contains(deliverNote.Status));

        query = query.OrderByDescending(x => x.CreatedOnUtc);

        var total = query.Count();
        if (total == 0)
        {
            return PagedDataDto.Create(new List<DeliveryNoteDto>(), pageIndex, pageSize, 0);
        }

        var deliveryNotes = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        return PagedDataDto.Create(deliveryNotes.Select(MapToDto).ToList(), pageIndex, pageSize, total);
    }

    public Task<IDictionary<Guid, decimal>> GetDeliveredQuantitiesAsync(IEnumerable<Guid> orderItemIds)
    {
        var ids = orderItemIds.ToList();
        if (ids.Count == 0)
        {
            return Task.FromResult<IDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
        }

        var deliveredQuantities = deliveryNoteReader.DataSource
            .Where(x => x.Status == DeliveryNoteStatus.Delivered)
            .SelectMany(x => x.Items)
            .Where(x => ids.Contains(x.OrderItemId))
            .GroupBy(x => x.OrderItemId)
            .Select(g => new { OrderItemId = g.Key, DeliveredQuantity = g.Sum(x => x.Quantity) })
            .ToList();

        IDictionary<Guid, decimal> result = deliveredQuantities.ToDictionary(x => x.OrderItemId, x => x.DeliveredQuantity);

        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = 0;
            }
        }

        return Task.FromResult(result);
    }

    public Task<IDictionary<Guid, List<DeliveryNoteLinkDto>>> GetDeliveryNoteLinksAsync(IEnumerable<Guid> orderItemIds)
    {
        var ids = orderItemIds.ToList();
        if (ids.Count == 0)
        {
            return Task.FromResult<IDictionary<Guid, List<DeliveryNoteLinkDto>>>(new Dictionary<Guid, List<DeliveryNoteLinkDto>>());
        }

        var links = deliveryNoteReader.DataSource
            .Where(x => x.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(x => x.Items.Select(i => new { i.OrderItemId, x.Id, x.Code, x.Status, x.CreatedOnUtc }))
            .Where(x => ids.Contains(x.OrderItemId))
            .ToList()
            .GroupBy(x => x.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new DeliveryNoteLinkDto(x.Id, x.Code, x.Status, x.CreatedOnUtc)).Distinct().ToList()
            );

        foreach (var id in ids)
        {
            if (!links.ContainsKey(id))
            {
                links[id] = [];
            }
        }

        return Task.FromResult<IDictionary<Guid, List<DeliveryNoteLinkDto>>>(links);
    }

    private async Task CreateCustomerReturnFromRejectedAcceptanceAsync(
        DeliveryNote deliveryNote, DeliveryAcceptanceResolution acceptance)
    {
        var rejectedLines = acceptance.Lines
            .Where(line => line.RejectedQuantity > 0)
            .ToList();
        if (rejectedLines.Count == 0)
            return;

        var deliveryItemsById = deliveryNote.Items.ToDictionary(item => item.Id);
        var itemDtos = rejectedLines
            .Select(line =>
            {
                var item = deliveryItemsById[line.DeliveryNoteItemId];
                return new CreateCustomerReturnItemDto
                {
                    ProductId = item.ProductId,
                    DeliveryNoteItemId = item.Id,
                    RequestedQuantity = line.RejectedQuantity,
                    AcceptedQuantity = line.RejectedQuantity,
                    OriginalUnitPrice = item.UnitPrice,
                    ReturnUnitPrice = item.UnitPrice
                };
            })
            .ToList();

        await customerReturnManager.CreateAsync(new CreateCustomerReturnDto
        {
            DeliveryNoteId = deliveryNote.Id,
            CustomerId = deliveryNote.CustomerId,
            WarehouseId = deliveryNote.IsDirectShip ? null : deliveryNote.WarehouseId,
            AdditionalCost = 0,
            Note = BuildAutoGeneratedReturnNote(deliveryNote, acceptance, rejectedLines, deliveryItemsById),
            Items = itemDtos
        }).ConfigureAwait(false);
    }

    private static string BuildAutoGeneratedReturnNote(
        DeliveryNote deliveryNote,
        DeliveryAcceptanceResolution acceptance,
        IReadOnlyCollection<DeliveryAcceptanceLine> rejectedLines,
        IReadOnlyDictionary<Guid, DeliveryNoteItem> deliveryItemsById)
    {
        var rejectedSummary = string.Join("; ", rejectedLines.Select(line =>
        {
            var item = deliveryItemsById[line.DeliveryNoteItemId];
            var reason = string.IsNullOrWhiteSpace(line.RejectReason) ? string.Empty : $" ({line.RejectReason!.Trim()})";
            return $"{item.ProductName}: {line.RejectedQuantity:#,##0.##}{reason}";
        }));

        if (acceptance.AgreedCustomerCharge == 0)
            return $"Khi nhận hàng từ phiếu {deliveryNote.Code}, khách hàng trả về: {rejectedSummary}.";

        var chargeReason = string.IsNullOrWhiteSpace(acceptance.AgreedCustomerChargeReason)
            ? string.Empty
            : $" ({acceptance.AgreedCustomerChargeReason!.Trim()})";
        return $"Khi nhận hàng từ phiếu {deliveryNote.Code}, khách hàng trả về: {rejectedSummary}. Chi phí phát sinh: {acceptance.AgreedCustomerCharge:#,##0.##}{chargeReason}.";
    }

    private static DeliveryAcceptanceResolution ResolveDeliveryAcceptance(
        DeliveryNote deliveryNote,
        DeliveryAcceptanceDto? acceptance)
    {
        var requestedByItemId = new Dictionary<Guid, DeliveryAcceptanceItemDto>();
        if (acceptance?.Items is not null)
        {
            foreach (var requestItem in acceptance.Items)
            {
                if (requestItem.DeliveryNoteItemId == Guid.Empty ||
                    !requestedByItemId.TryAdd(requestItem.DeliveryNoteItemId, requestItem))
                {
                    throw new NamEcommerceDomainException("Error.DeliveryAcceptance.InvalidItem");
                }
            }
        }

        var deliveryItemsById = deliveryNote.Items.ToDictionary(item => item.Id);
        if (requestedByItemId.Keys.Any(itemId => !deliveryItemsById.ContainsKey(itemId)))
            throw new NamEcommerceDomainException("Error.DeliveryAcceptance.InvalidItem");

        var lines = new List<DeliveryAcceptanceLine>(deliveryNote.Items.Count);
        decimal acceptedGoodsAmount = 0;
        foreach (var item in deliveryNote.Items)
        {
            var hasRequested = requestedByItemId.TryGetValue(item.Id, out var requestItem);
            var acceptedQuantity = hasRequested ? requestItem!.AcceptedQuantity : item.Quantity;
            var rejectedQuantity = hasRequested ? requestItem!.RejectedQuantity : 0m;
            var rejectReason = hasRequested ? requestItem!.RejectReason : null;

            if (acceptedQuantity < 0 || rejectedQuantity < 0)
                throw new NamEcommerceDomainException("Error.DeliveryAcceptance.NegativeQuantity");

            var totalQuantity = acceptedQuantity + rejectedQuantity;
            if (!SameQuantity(totalQuantity, item.Quantity))
                throw new NamEcommerceDomainException("Error.DeliveryAcceptance.QuantityMismatch", item.ProductName);

            if (rejectedQuantity > 0 && string.IsNullOrWhiteSpace(rejectReason))
                throw new NamEcommerceDomainException("Error.DeliveryAcceptance.RejectReasonRequired", item.ProductName);

            lines.Add(new DeliveryAcceptanceLine(item.Id, acceptedQuantity, rejectedQuantity, rejectReason?.Trim()));
            acceptedGoodsAmount += acceptedQuantity * item.UnitPrice;
        }

        var agreedCustomerCharge = acceptance?.AgreedCustomerCharge ?? 0m;
        var amountToCollect = Math.Max(0m, acceptedGoodsAmount + deliveryNote.Surcharge + agreedCustomerCharge);
        return new DeliveryAcceptanceResolution(
            amountToCollect,
            agreedCustomerCharge,
            acceptance?.AgreedCustomerChargeReason,
            lines);
    }

    private static bool SameQuantity(decimal left, decimal right)
        => Math.Abs(left - right) <= 0.0001m;

    private async Task MarkRelatedOrderItemsReceivedByCustomerAsync(DeliveryNote deliveryNote)
    {
        var order = await orderReader.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
        if (order is null)
            return;

        var deliveredQuantitiesByOrderItem = GetNetDeliveredQuantitiesByOrderItem(order.Id);

        foreach (var noteItem in deliveryNote.Items.Where(item => item.OrderItemId != Guid.Empty))
        {
            var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == noteItem.OrderItemId);
            var deliveredQuantity = deliveredQuantitiesByOrderItem.GetValueOrDefault(noteItem.OrderItemId);
            if (orderItem != null && !orderItem.IsDelivered && deliveredQuantity >= orderItem.Quantity)
            {
                await orderManager.MarkOrderItemReceivedByCustomerAsync(new MarkOrderItemReceivedByCustomerDto
                {
                    OrderId = order.Id,
                    OrderItemId = orderItem.Id
                }).ConfigureAwait(false);
            }
        }
    }

    private void EnsureQuantitiesCanBeDelivered(
        Order order,
        IReadOnlyDictionary<Guid, decimal> requestedQuantitiesByOrderItem,
        bool includeReturnedCompensation)
    {
        var orderItemsById = order.OrderItems.ToDictionary(item => item.Id);
        var requestedOrderItemIds = requestedQuantitiesByOrderItem.Keys.ToList();
        var activeDeliveryQuantitiesByOrderItem = GetActiveDeliveryQuantitiesByOrderItem(
            order.Id,
            requestedOrderItemIds,
            includeReturnedCompensation);
        var directShipOutstandingQuantitiesByOrderItem = GetDirectShipOutstandingQuantitiesByOrderItem(requestedOrderItemIds);

        foreach (var (orderItemId, requestedQuantity) in requestedQuantitiesByOrderItem)
        {
            if (!orderItemsById.TryGetValue(orderItemId, out var orderItem))
                throw new OrderItemIsNotFoundException();

            var alreadyInDeliveryNotes = activeDeliveryQuantitiesByOrderItem.GetValueOrDefault(orderItemId);
            var directShipOutstanding = directShipOutstandingQuantitiesByOrderItem.GetValueOrDefault(orderItemId);
            var remainingQuantity = orderItem.Quantity - alreadyInDeliveryNotes - directShipOutstanding;
            if (requestedQuantity > remainingQuantity)
            {
                throw new NamEcommerceDomainException(
                    "Error.QuantityExceedsRemaining",
                    orderItem.ProductName ?? string.Empty,
                    Math.Max(0m, remainingQuantity));
            }
        }
    }

    private Dictionary<Guid, decimal> GetActiveDeliveryQuantitiesByOrderItem(
        Guid orderId,
        IReadOnlyCollection<Guid> orderItemIds,
        bool includeReturnedCompensation)
    {
        if (orderItemIds.Count == 0)
            return [];

        var deliveredByOrderItem = deliveryNoteReader.DataSource
            .Where(note => note.OrderId == orderId && note.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(note => note.Items)
            .Where(item => orderItemIds.Contains(item.OrderItemId))
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity));

        if (!includeReturnedCompensation)
        {
            foreach (var orderItemId in orderItemIds)
            {
                if (!deliveredByOrderItem.ContainsKey(orderItemId))
                    deliveredByOrderItem[orderItemId] = 0m;
            }

            return deliveredByOrderItem;
        }

        var returnedByOrderItem = GetReturnedQuantitiesByOrderItem(orderId, orderItemIds);
        foreach (var orderItemId in orderItemIds)
        {
            deliveredByOrderItem.TryGetValue(orderItemId, out var deliveredQuantity);
            returnedByOrderItem.TryGetValue(orderItemId, out var returnedQuantity);
            deliveredByOrderItem[orderItemId] = Math.Max(0m, deliveredQuantity - returnedQuantity);
        }

        return deliveredByOrderItem;
    }

    private Dictionary<Guid, decimal> GetNetDeliveredQuantitiesByOrderItem(Guid orderId)
    {
        var deliveredByOrderItem = deliveryNoteReader.DataSource
            .Where(note => note.OrderId == orderId && note.Status == DeliveryNoteStatus.Delivered)
            .SelectMany(note => note.Items)
            .Where(item => item.OrderItemId != Guid.Empty)
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity));

        var orderItemIds = deliveredByOrderItem.Keys.ToList();
        var returnedByOrderItem = GetReturnedQuantitiesByOrderItem(orderId, orderItemIds);
        foreach (var orderItemId in orderItemIds)
        {
            var deliveredQuantity = deliveredByOrderItem.GetValueOrDefault(orderItemId);
            var returnedQuantity = returnedByOrderItem.GetValueOrDefault(orderItemId);
            deliveredByOrderItem[orderItemId] = Math.Max(0m, deliveredQuantity - returnedQuantity);
        }

        return deliveredByOrderItem;
    }

    private Dictionary<Guid, decimal> GetReturnedQuantitiesByOrderItem(Guid orderId, IReadOnlyCollection<Guid> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return orderItemIds.ToDictionary(id => id, id => 0m);

        // 1. Chuẩn bị Query lấy các DeliveryNoteItem hợp lệ (chưa thực thi dưới DB - vẫn là IQueryable)
        var validDeliveryNoteItems = deliveryNoteReader.DataSource
            .Where(note => note.OrderId == orderId)
            .SelectMany(note => note.Items)
            .Where(item => item.OrderItemId != Guid.Empty && orderItemIds.Contains(item.OrderItemId))
            .Select(item => new { item.Id, item.OrderItemId });

        // 2. Thực hiện Join và GroupBy dưới DB, sau đó lấy kết quả về RAM dạng Dictionary
        var returnedByOrderItem = customerReturnReader.DataSource
            .Where(returnNote => returnNote.Status != CustomerReturnStatus.Cancelled)
            .SelectMany(returnNote => returnNote.Items)
            .Where(returnItem => returnItem.DeliveryNoteItemId.HasValue)
            // Join trực tiếp 2 Queryable với nhau dưới DB thông qua Id của DeliveryNoteItem
            .Join(
                validDeliveryNoteItems,
                returnItem => returnItem.DeliveryNoteItemId!.Value,
                dnItem => dnItem.Id,
                (returnItem, dnItem) => new { dnItem.OrderItemId, returnItem.AcceptedQuantity }
            )
            // Group theo OrderItemId để tính tổng
            .GroupBy(x => x.OrderItemId)
            // Đến đây EF sẽ sinh ra SQL, chạy dưới DB và trả kết quả về RAM dạng Dictionary
            .ToDictionary(
                group => group.Key,
                group => group.Sum(x => x.AcceptedQuantity)
            );

        // 3. Đảm bảo tất cả orderItemId truyền vào đều có mặt trong kết quả (nếu không có thì bằng 0)
        foreach (var orderItemId in orderItemIds)
        {
            if (!returnedByOrderItem.ContainsKey(orderItemId))
                returnedByOrderItem[orderItemId] = 0m;
        }

        return returnedByOrderItem;
    }

    private Dictionary<Guid, decimal> GetDirectShipOutstandingQuantitiesByOrderItem(IReadOnlyCollection<Guid> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return [];

        return allocationReader.DataSource
            .Where(allocation => allocation.IsDirectShip
                && allocation.Status != AllocationStatus.Cancelled
                && orderItemIds.Contains(allocation.OrderItemId))
            .ToList()
            .GroupBy(allocation => allocation.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity)));
    }

    private sealed record DeliveryAcceptanceLine(
        Guid DeliveryNoteItemId,
        decimal AcceptedQuantity,
        decimal RejectedQuantity,
        string? RejectReason);

    private sealed record DeliveryAcceptanceResolution(
        decimal AmountToCollect,
        decimal AgreedCustomerCharge,
        string? AgreedCustomerChargeReason,
        IReadOnlyList<DeliveryAcceptanceLine> Lines);

    private static DeliveryNoteDto MapToDto(DeliveryNote deliveryNote)
    {
        return new DeliveryNoteDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderId = deliveryNote.OrderId,
            WarehouseId = deliveryNote.WarehouseId,
            OrderCode = deliveryNote.OrderCode,
            CustomerId = deliveryNote.CustomerId,
            CustomerName = deliveryNote.CustomerInfo.FullName,
            CustomerPhone = deliveryNote.CustomerInfo.PhoneNumber,
            CustomerAddress = deliveryNote.CustomerInfo.Address,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = deliveryNote.Status,
            SourceType = deliveryNote.SourceType,
            IsDirectShip = deliveryNote.IsDirectShip,
            DeliveryConfirmationStatus = deliveryNote.DeliveryConfirmationStatus,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            DeliveryProofPictureId = deliveryNote.DeliveryProofPictureId,
            DeliveryReceiverName = deliveryNote.DeliveryReceiverName,
            CreatedByUserId = deliveryNote.CreatedByUserId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            UpdatedOnUtc = deliveryNote.UpdatedOnUtc,
            TotalAmount = deliveryNote.TotalAmount,
            Surcharge = deliveryNote.Surcharge,
            SurchargeReason = deliveryNote.SurchargeReason,
            AmountToCollect = deliveryNote.AmountToCollect,
            Items = deliveryNote.Items.Select(i => new DeliveryNoteItemDto
            {
                Id = i.Id,
                DeliveryNoteId = i.DeliveryNoteId,
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                ProductName = i.ProductName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                CostAtDispatch = i.CostAtDispatch
            }).ToList()
        };
    }
}
