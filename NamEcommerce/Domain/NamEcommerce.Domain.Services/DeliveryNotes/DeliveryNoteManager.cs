using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Debts;
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
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Orders;
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
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Values;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.DeliveryNotes;

namespace NamEcommerce.Domain.Services.DeliveryNotes;

public sealed class DeliveryNoteManager(
    IRepository<DeliveryNote> deliveryNoteRepository,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<Order> orderReader, IRepository<Order> orderRepository,
    IEntityDataReader<PurchaseOrderItemAllocation> allocationReader,
    IOrderManager orderManager,
    IInventoryStockManager stockManager,
    IInventoryCostingManager inventoryCostingManager,
    IProductReservationManager productReservationManager,
    IEntityDataReader<CustomerReturn> customerReturnReader,
    IEntityDataReader<VendorReturn> vendorReturnReader,
    IEntityDataReader<CustomerPayment> customerPaymentReader,
    IEntityDataReader<Customer> customerReader,
    ICustomerReturnManager customerReturnManager,
    IEntityDataReader<DeliveryRun> deliveryRunReader,
    IRepository<DeliveryRun> deliveryRunRepository,
    EntityCodeGenerator entityCodeGenerator) : IDeliveryNoteManager
{
    private Task<string> GenerateCodeAsync()
    {
        var prefix = $"{DeliveryNote.CODE_PREFIX}-{DateTime.UtcNow:yyMM}";
        return entityCodeGenerator.NextAsync(prefix, () => deliveryNoteReader.SecuredDataSource.CountAsync(d => d.Code.StartsWith(prefix)));
    }

    private async Task<decimal> GetDisplayCostAsync(Guid productId)
    {
        var summary = await inventoryCostingManager.GetCurrentCostSummaryAsync(productId).ConfigureAwait(false);
        return summary.AverageCost;
    }

    public async Task<DeliveryNoteDto> CreateFromOrderAsync(CreateDeliveryNoteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var order = await orderRepository.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new OrderIsNotFoundException(dto.OrderId);

        var orderItemsById = order.OrderItems.ToDictionary(item => item.Id);
        var requestedQuantitiesByOrderItem = dto.Items
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(g => (SecondaryItemId)(order.Id, g.Key), g => g.Sum(item => item.Quantity));

        await EnsureQuantitiesCanBeDeliveredAsync(order, requestedQuantitiesByOrderItem).ConfigureAwait(false);

        var productCollectedAmount = dto.AmountToCollect - dto.Surcharge;
        if (productCollectedAmount > 0)
        {
            var deliveryNotes = await deliveryNoteReader.DataSource
                .Where(dn => dn.OrderId == dto.OrderId && dn.Status != DeliveryNoteStatus.Cancelled)
                .ToListAsync().ConfigureAwait(false);
            var paidForOrder = await customerPaymentReader.DataSource
                .Where(p => p.OrderId == dto.OrderId)
                .SumAsync(p => p.Amount).ConfigureAwait(false)
                - deliveryNotes.Sum(dn => dn.Surcharge);
            var remaining = order.OrderTotal - paidForOrder;
            if (productCollectedAmount > remaining)
                throw new AmountToCollectExceedsOrderRemainingException(dto.AmountToCollect, Math.Max(0m, remaining));
        }

        var itemsByProductWarehouse = dto.Items
            .GroupBy(item => new { orderItemsById[item.OrderItemId].ProductId, item.WarehouseId })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Quantity = g.Sum(item => item.Quantity) })
            .ToList();
        foreach (var item in itemsByProductWarehouse)
        {
            var stock = await stockManager.GetInventoryStockForProductAsync(item.ProductId, item.WarehouseId).ConfigureAwait(false);
            if (stock is null)
                throw new InsufficientStockException(item.ProductId, item.WarehouseId, item.Quantity, 0);
            if (stock.QuantityAvailable < item.Quantity)
                throw new InsufficientStockException(item.ProductId, item.WarehouseId, item.Quantity, stock.QuantityAvailable);
        }

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        var deliveryNote = new DeliveryNote(code, order.Id, order.CustomerId, dto.AmountToCollect, dto.Surcharge)
        {
            ShippingAddress = dto.ShippingAddress,
            ShippingPhoneNumber = string.IsNullOrWhiteSpace(dto.ShippingPhoneNumber)
                ? order.ShippingPhoneNumber ?? order.CustomerInfo.PhoneNumber
                : dto.ShippingPhoneNumber,
            ShowPrice = dto.ShowPrice,
            SurchargeReason = dto.SurchargeReason,
            Note = dto.Note,
            AppliedOrderDiscount = dto.AppliedOrderDiscount,
            AppliedOrderPrepaid = dto.AppliedOrderPrepaid,
            CustomerInfo = new CustomerInfo(order.CustomerInfo.FullName, order.CustomerInfo.PhoneNumber, order.CustomerInfo.Address)
            {
                IsRetailWalkInCustomer = order.CustomerInfo.IsRetailWalkInCustomer
            },
            OrderCode = order.Code,
            CreatedByUserId = order.CreatedByUserId
        };
        if (order.ProcessRequiresPayment && !order.HasPayments())
            deliveryNote.RequiresPayment();
        foreach (var itemDto in dto.Items)
        {
            var orderItem = orderItemsById[itemDto.OrderItemId];

            deliveryNote.AddItem(
                orderItemId: orderItem.Id,
                productId: orderItem.ProductId,
                productName: orderItem.ProductName ?? string.Empty,
                quantity: itemDto.Quantity,
                unitPrice: orderItem.UnitPrice,
                warehouseId: itemDto.WarehouseId
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

        if (deliveryNote.RequiresPaymentToConfirm && !deliveryNote.HasPaid())
            throw new DeliveryNoteCannotChangeStatusException(deliveryNote.Status, DeliveryNoteStatus.Confirmed);

        if (deliveryNote.SourceType == DeliveryNoteSourceType.ToCustomer && deliveryNote.OrderId != Guid.Empty)
        {
            var order = await orderRepository.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
            if (order is null)
                throw new OrderIsNotFoundException(deliveryNote.OrderId);
            if (order is { OrderStatus: OrderStatus.Completed or OrderStatus.Cancelled })
                throw new DeliveryNoteOrderAlreadyClosedException(deliveryNote.OrderId, order.OrderStatus);
            if (!order.CanProcess())
                throw new OrderCannotProcessException();
        }

        deliveryNote.Confirm();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task MarkDeliveringAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        foreach (var item in deliveryNote.Items)
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);

        deliveryNote.MarkDelivering();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task AssignDeliveryUserAsync(AssignDeliveryUserDto dto)
    {
        dto.Verify();

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        deliveryNote.AssignDeliveryUser(
            dto.AssignedDeliveryUserId,
            dto.AssignedDeliveryUsername,
            dto.AssignedDeliveryFullName,
            DateTime.UtcNow);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task UpdateShippingAsync(UpdateDeliveryNoteShippingDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        if (!deliveryNote.CanEditShippingInfo())
            throw new DeliveryNoteCannotUpdateShippingInfoException();

        deliveryNote.UpdateShippingInfo(dto.ShippingAddress, dto.ShippingPhoneNumber);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task MarkDeliveredAsync(MarkDeliveryNoteDeliveredDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        if (deliveryNote.Status == DeliveryNoteStatus.Delivered
            && deliveryNote.HasSameDeliveryCompletionRequest(dto.CompletionMetadata?.IdempotencyKey))
        {
            return;
        }

        var acceptanceDto = dto.Acceptance ?? BuildAcceptanceFromStoredSettlement(deliveryNote);
        var acceptance = ResolveDeliveryAcceptance(deliveryNote, acceptanceDto);
        var completionMetadata = MergeCompletionMetadata(deliveryNote, dto.CompletionMetadata);
        if (completionMetadata?.CashCollectedAmount > acceptance.AmountToCollect)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotExceedAmountToCollect");

        // Khách lẻ (tài khoản dùng chung) không được để công nợ dương: phải thu đủ số còn lại tại lúc giao.
        if (deliveryNote.CustomerInfo.IsRetailWalkInCustomer
            && (completionMetadata?.CashCollectedAmount ?? 0m) < acceptance.AmountToCollect)
        {
            throw new RetailOrderCannotLeaveDebtException();
        }

        // Shipper (mobile) không được tự hoàn tất khi thu hụt — phải qua duyệt admin trước.
        var isMobileShipper = string.Equals(completionMetadata?.Source, "MobilePwa", StringComparison.OrdinalIgnoreCase);
        var hasShortfall = acceptance.RejectedGoodsAmount > 0 ||
            (completionMetadata?.CashCollectedAmount ?? 0m) < acceptance.AmountToCollect;
        if (isMobileShipper && hasShortfall && deliveryNote.SettlementApproval != DeliverySettlementApprovalStatus.Approved)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.ApprovalRequired");

        // Snapshot chỉ cập nhật khi phiếu chưa xuất kho (Confirmed → Delivered trực tiếp).
        // Nếu đang ở Delivering, CostAtDispatch đã được set đúng trước khi xuất kho ở MarkDeliveringAsync.
        // Đọc lại sau khi xuất sẽ trả về 0 vì balance cost ledger đã về 0.
        if (deliveryNote.Status == DeliveryNoteStatus.Confirmed)
        {
            foreach (var item in deliveryNote.Items)
            {
                item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
            }
        }

        deliveryNote.AmountToCollect = acceptance.AmountToCollect;
        var pictureIds = ResolveProofPictureIds(deliveryNote, dto.PictureIds);
        var receiverName = string.IsNullOrWhiteSpace(dto.ReceiverName)
            ? deliveryNote.DeliveryReceiverName
            : dto.ReceiverName;

        // 1. Mark DeliveryNote Delivered — raise DeliveryNoteDelivered event
        deliveryNote.MarkDelivered(
            pictureIds,
            receiverName,
            completionMetadata?.Latitude,
            completionMetadata?.Longitude,
            completionMetadata?.LocationAddress,
            completionMetadata?.Note,
            completionMetadata?.Source,
            completionMetadata?.IdempotencyKey,
            completionMetadata?.CashCollectedAmount,
            acceptance.RejectedGoodsAmount,
            acceptance.DebtAmount);
        // Save entity (display cost + status) → interceptor fires events.
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        // 2. Nếu khách không nhận đủ hàng, tạo phiếu CustomerReturn draft tự động để vào bước kiểm hàng/xác nhận.
        await CreateCustomerReturnFromRejectedAcceptanceAsync(deliveryNote, acceptance).ConfigureAwait(false);

        // 2. Mark related OrderItems as Delivered only when the full ordered quantity has been delivered.
        var order = await orderRepository.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
        if (order is not null)
        {
            var deliveredQuantitiesByOrderItem = await GetNetDeliveredQuantitiesByOrderItemAsync(order.Id, deliveryNote).ConfigureAwait(false);

            foreach (var noteItem in deliveryNote.Items.Where(item => item.OrderItemId != Guid.Empty))
            {
                var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == noteItem.OrderItemId);
                var deliveredQuantity = deliveredQuantitiesByOrderItem.GetValueOrDefault((SecondaryItemId)(order.Id, noteItem.OrderItemId));
                if (orderItem != null && !orderItem.IsDelivered && deliveredQuantity >= orderItem.Quantity)
                {
                    await orderManager.MarkOrderItemDeliveredAsync(new MarkOrderItemDeliveredDto
                    {
                        OrderId = order.Id,
                        OrderItemId = orderItem.Id,
                        PictureId = pictureIds.Count > 0 ? pictureIds[0] : Guid.Empty
                    }).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task MarkPendingConfirmationAsync(MarkDeliveryNoteDeliveredDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        if (deliveryNote.Status == DeliveryNoteStatus.PendingConfirmation
            && deliveryNote.HasSameDeliveryCompletionRequest(dto.CompletionMetadata?.IdempotencyKey))
        {
            return;
        }

        var acceptance = ResolveDeliveryAcceptance(deliveryNote, dto.Acceptance);
        if (dto.CompletionMetadata?.CashCollectedAmount > acceptance.AmountToCollect)
            throw new NamEcommerceDomainException("Error.CashCollectedAmountCannotExceedAmountToCollect");

        if (deliveryNote.Status == DeliveryNoteStatus.Confirmed)
        {
            foreach (var item in deliveryNote.Items)
            {
                item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
            }
        }

        deliveryNote.AmountToCollect = acceptance.AmountToCollect;
        var acceptanceLines = acceptance.Lines.Select(line =>
            (line.DeliveryNoteItemId, line.AcceptedQuantity, line.RejectedQuantity, line.RejectReason));

        deliveryNote.MarkPendingConfirmation(
            dto.PictureIds,
            dto.ReceiverName,
            dto.CompletionMetadata?.Latitude,
            dto.CompletionMetadata?.Longitude,
            dto.CompletionMetadata?.LocationAddress,
            dto.CompletionMetadata?.Note,
            dto.CompletionMetadata?.Source,
            dto.CompletionMetadata?.IdempotencyKey,
            dto.CompletionMetadata?.CashCollectedAmount,
            acceptanceLines);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task MarkReceivedByCustomerAsync(
        Guid id, DateTime receivedAtUtc, string? receiverName,
        string? note, DeliveryAcceptanceDto? acceptance = null)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        DeliveryAcceptanceResolution? resolvedAcceptance = null;
        if (deliveryNote.Status != DeliveryNoteStatus.Delivered)
        {
            resolvedAcceptance = ResolveDeliveryAcceptance(deliveryNote, acceptance);
            if (deliveryNote.Status == DeliveryNoteStatus.Confirmed)
            {
                foreach (var item in deliveryNote.Items)
                {
                    item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
                }
            }

            deliveryNote.AmountToCollect = resolvedAcceptance.AmountToCollect;
        }

        var transitionedToDelivered = deliveryNote.MarkReceivedByCustomer(
            receivedAtUtc,
            receiverName,
            note,
            resolvedAcceptance?.RejectedGoodsAmount ?? 0,
            resolvedAcceptance?.DebtAmount);
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
        var deliveryNote = new DeliveryNote(code, Guid.Empty, Guid.Empty, 0, 0)
        {
            SourceType = DeliveryNoteSourceType.ToVendorReturn,
            CreatedByUserId = vendorReturn.CreatedByUserId
        };

        foreach (var item in dto.Items)
            deliveryNote.AddItemFromVendorReturn(item.ProductId, item.ProductName, item.Quantity, item.UnitCost, dto.WarehouseId);

        foreach (var item in deliveryNote.Items)
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);

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

        var order = await orderReader.DataSource
            .FirstOrDefaultAsync(o => o.OrderItems.Any(oi => oi.Id == dto.OrderItemId)).ConfigureAwait(false)
            ?? throw new OrderIsNotFoundException(dto.OrderItemId);

        var orderItem = order.OrderItems.First(oi => oi.Id == dto.OrderItemId);
        await EnsureQuantitiesCanBeDeliveredAsync(order,
            new Dictionary<SecondaryItemId, decimal>
            {
                [(SecondaryItemId)(order.Id, orderItem.Id)] = dto.Quantity
            }, includeDirectShipOutstanding: false).ConfigureAwait(false);

        var code = await GenerateCodeAsync().ConfigureAwait(false);
        string contactName = string.IsNullOrWhiteSpace(dto.ContactName)
            ? order.CustomerInfo.FullName
            : dto.ContactName.Trim();
        string contactPhone = string.IsNullOrWhiteSpace(dto.ContactPhone)
            ? order.CustomerInfo.PhoneNumber
            : dto.ContactPhone.Trim();

        var deliveryNote = new DeliveryNote(code, order.Id, order.CustomerId, 0, 0)
        {
            OrderCode = order.Code,
            CustomerInfo = new CustomerInfo(contactName, contactPhone, dto.ShippingAddress)
            {
                IsRetailWalkInCustomer = order.CustomerInfo.IsRetailWalkInCustomer
            },
            ShippingAddress = dto.ShippingAddress,
            ShippingPhoneNumber = contactPhone
        };

        deliveryNote.AddItem(
            orderItemId: orderItem.Id,
            productId: orderItem.ProductId,
            productName: orderItem.ProductName ?? string.Empty,
            quantity: dto.Quantity,
            unitPrice: orderItem.UnitPrice,
            warehouseId: dto.DirectShipWarehouseId);

        deliveryNote.SetAsDirectShip(dto.GoodsReceiptId);
        deliveryNote.MarkCreated();
        deliveryNote.Confirm();

        var inserted = await deliveryNoteRepository.InsertAsync(deliveryNote).ConfigureAwait(false);

        return inserted.Id;
    }

    public async Task ConfirmDirectShipDeliveryAsync(Guid id, DateTime confirmedAtUtc, string? note, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        foreach (var item in deliveryNote.Items)
        {
            item.CostAtDispatch = await GetDisplayCostAsync(item.ProductId).ConfigureAwait(false);
        }

        deliveryNote.ConfirmDirectShipDelivery(confirmedAtUtc, note);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task RejectDirectShipDeliveryAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        deliveryNote.RejectDirectShipDelivery(reason);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
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

        var linkedReturns = await customerReturnReader.DataSource
            .Where(r => r.DeliveryNoteId == id)
            .Select(r => new { r.Id, r.Status })
            .ToListAsync().ConfigureAwait(false);

        if (linkedReturns.Any(r => r.Status == CustomerReturnStatus.Confirmed))
            throw new DeliveryNoteHasConfirmedReturnsException(id);

        var cancellableReturnIds = linkedReturns
            .Where(r => r.Status == CustomerReturnStatus.Draft || r.Status == CustomerReturnStatus.Inspecting)
            .Select(r => r.Id)
            .ToList();

        foreach (var returnId in cancellableReturnIds)
            await customerReturnManager.CancelAsync(returnId).ConfigureAwait(false);

        bool wasBeforeDispatch = deliveryNote.Status == DeliveryNoteStatus.Draft || deliveryNote.Status == DeliveryNoteStatus.Confirmed;
        bool wasDelivering = deliveryNote.Status == DeliveryNoteStatus.Delivering
            || deliveryNote.Status == DeliveryNoteStatus.PendingConfirmation;

        deliveryNote.Cancel();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        if (wasBeforeDispatch && deliveryNote.SourceType == DeliveryNoteSourceType.ToCustomer && !deliveryNote.IsDirectShip)
        {
            var itemsByProduct = deliveryNote.Items
                .GroupBy(item => item.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(item => item.Quantity) })
                .ToList();
            var itemsByProductWarehouse = deliveryNote.Items
                .GroupBy(item => new
                {
                    item.ProductId,
                    WarehouseId = ResolveItemWarehouseId(item)
                })
                .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Quantity = g.Sum(item => item.Quantity) })
                .ToList();

            foreach (var item in itemsByProductWarehouse)
            {
                await ReleaseReservedStockIfPresentAsync(
                    item.ProductId,
                    item.WarehouseId,
                    item.Quantity,
                    deliveryNote.Id,
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
                    await RestoreOrderReservationForCancelledDeliveryAsync(
                        item.ProductId,
                        quantityToReserve,
                        deliveryNote.OrderId,
                        deliveryNote.Id).ConfigureAwait(false);
                }
            }
        }
        else if (wasDelivering && deliveryNote.SourceType == DeliveryNoteSourceType.ToCustomer && !deliveryNote.IsDirectShip)
        {
            foreach (var item in deliveryNote.Items)
            {
                await stockManager.ReceiveStockUpToAsync(
                    item.ProductId,
                    ResolveItemWarehouseId(item),
                    deliveryNote.Items
                        .Where(i => i.ProductId == item.ProductId
                            && ResolveItemWarehouseId(i) == ResolveItemWarehouseId(item))
                        .Sum(i => i.Quantity),
                    $"Nhập lại kho do hủy phiếu xuất {deliveryNote.Code}",
                    Guid.Empty,
                    (int)StockReferenceType.SalesOrder,
                    deliveryNote.Id).ConfigureAwait(false);

                await inventoryCostingManager.RegisterInboundAsync(new RegisterInventoryInboundCostDto
                {
                    ProductId = item.ProductId,
                    WarehouseId = ResolveItemWarehouseId(item),
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
                    await RestoreOrderReservationForCancelledDeliveryAsync(
                        item.ProductId,
                        item.Quantity,
                        deliveryNote.OrderId,
                        deliveryNote.Id).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task RequestSettlementApprovalAsync(RequestDeliverySettlementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        var acceptance = ResolveDeliveryAcceptance(deliveryNote, dto.Acceptance);

        decimal proposed = acceptance.RejectedGoodsAmount > 0
            ? Math.Max(0m, deliveryNote.AmountToCollect - acceptance.RejectedGoodsAmount)
            : deliveryNote.AmountToCollect;
        if (dto.ProposedAmountToCollect.HasValue)
            proposed = Math.Min(proposed, Math.Max(0m, dto.ProposedAmountToCollect.Value));

        var lines = acceptance.Lines.Select(line =>
            (line.DeliveryNoteItemId, line.AcceptedQuantity, line.RejectedQuantity, line.RejectReason));

        deliveryNote.RequestSettlementApproval(
            proposed, dto.Reason, dto.PictureIds, dto.ReceiverName, lines,
            dto.RequestedByUserId, DateTime.UtcNow);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task ApproveSettlementAsync(ApproveDeliverySettlementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        deliveryNote.ApproveSettlement(
            dto.ApprovedAmountToCollect, dto.AgreedCustomerCharge, dto.AgreedCustomerChargeReason,
            dto.AdminNote, dto.ApprovedByUserId, DateTime.UtcNow);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task RejectSettlementAsync(Guid id, string reason, Guid? approvedByUserId)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);

        deliveryNote.RejectSettlement(reason, approvedByUserId, DateTime.UtcNow);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        // Auto: nhập lại kho + restore reservation + hủy phiếu (luồng Cancel hiện có).
        await CancelAsync(id).ConfigureAwait(false);
    }

    public async Task CompleteApprovedSettlementAsync(
        Guid id, IReadOnlyList<Guid> pictureIds, DeliveryCompletionMetadataDto? completionMetadata)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(id);
        if (deliveryNote.SettlementApproval != DeliverySettlementApprovalStatus.Approved)
            throw new NamEcommerceDomainException("Error.DeliverySettlement.NotApproved");

        var acceptance = new DeliveryAcceptanceDto
        {
            AgreedCustomerCharge = deliveryNote.ApprovedAgreedCustomerCharge ?? 0m,
            AgreedCustomerChargeReason = deliveryNote.ApprovedAgreedChargeReason,
            CompensateInNextDelivery = false,
            Items = deliveryNote.SettlementItems.Select(item => new DeliveryAcceptanceItemDto
            {
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                AcceptedQuantity = item.AcceptedQuantity,
                RejectedQuantity = item.RejectedQuantity,
                RejectReason = item.RejectReason
            }).ToList()
        };

        var proofPictureIds = pictureIds is { Count: > 0 }
            ? pictureIds
            : deliveryNote.DeliveryProofPictureIds.ToList();

        await MarkPendingConfirmationAsync(new MarkDeliveryNoteDeliveredDto
        {
            DeliveryNoteId = id,
            PictureIds = proofPictureIds,
            ReceiverName = deliveryNote.DeliveryReceiverName,
            Acceptance = acceptance,
            CompletionMetadata = new DeliveryCompletionMetadataDto
            {
                Latitude = completionMetadata?.Latitude,
                Longitude = completionMetadata?.Longitude,
                LocationAddress = completionMetadata?.LocationAddress,
                Note = completionMetadata?.Note,
                Source = completionMetadata?.Source,
                IdempotencyKey = completionMetadata?.IdempotencyKey,
                CashCollectedAmount = deliveryNote.ApprovedAmountToCollect ?? 0m
            }
        }).ConfigureAwait(false);
    }

    private static IReadOnlyList<Guid> ResolveProofPictureIds(DeliveryNote deliveryNote, IReadOnlyList<Guid> pictureIds)
        => pictureIds.Count > 0 ? pictureIds : deliveryNote.DeliveryProofPictureIds.ToList();

    private static DeliveryCompletionMetadataDto? MergeCompletionMetadata(
        DeliveryNote deliveryNote, DeliveryCompletionMetadataDto? completionMetadata)
    {
        if (completionMetadata is null && !deliveryNote.DeliveryCashCollectedAmount.HasValue)
            return null;

        return new DeliveryCompletionMetadataDto
        {
            Latitude = completionMetadata?.Latitude ?? deliveryNote.DeliveryLatitude,
            Longitude = completionMetadata?.Longitude ?? deliveryNote.DeliveryLongitude,
            LocationAddress = completionMetadata?.LocationAddress ?? deliveryNote.DeliveryLocationAddress,
            Note = completionMetadata?.Note ?? deliveryNote.DeliveryCompletionNote,
            Source = completionMetadata?.Source ?? deliveryNote.DeliveryCompletionSource,
            IdempotencyKey = completionMetadata?.IdempotencyKey ?? deliveryNote.DeliveryCompletionIdempotencyKey,
            CashCollectedAmount = completionMetadata?.CashCollectedAmount ?? deliveryNote.DeliveryCashCollectedAmount
        };
    }

    private static DeliveryAcceptanceDto? BuildAcceptanceFromStoredSettlement(DeliveryNote deliveryNote)
    {
        if (deliveryNote.SettlementItems.Count == 0)
            return null;

        return new DeliveryAcceptanceDto
        {
            AgreedCustomerCharge = deliveryNote.ApprovedAgreedCustomerCharge ?? 0m,
            AgreedCustomerChargeReason = deliveryNote.ApprovedAgreedChargeReason,
            CompensateInNextDelivery = false,
            Items = deliveryNote.SettlementItems.Select(item => new DeliveryAcceptanceItemDto
            {
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                AcceptedQuantity = item.AcceptedQuantity,
                RejectedQuantity = item.RejectedQuantity,
                RejectReason = item.RejectReason
            }).ToList()
        };
    }

    private static Guid ResolveItemWarehouseId(DeliveryNoteItem item)
    {
        if (item.WarehouseId == Guid.Empty)
            throw new WarehouseIsNotSuitableException(Guid.Empty);

        return item.WarehouseId;
    }

    private async Task ReleaseReservedStockIfPresentAsync(Guid productId, Guid warehouseId,
        decimal targetQuantity, Guid deliveryNoteId, string note)
    {
        if (targetQuantity <= 0)
            return;

        var stock = await stockManager.GetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            return;

        await stockManager.ReleaseReservedStockAsync(
            productId,
            warehouseId,
            targetQuantity,
            deliveryNoteId,
            Guid.Empty,
            note).ConfigureAwait(false);
    }

    private async Task RestoreOrderReservationForCancelledDeliveryAsync(Guid productId,
        decimal targetQuantity, Guid orderId, Guid deliveryNoteId)
    {
        if (orderId == Guid.Empty || targetQuantity <= 0)
            return;

        var alreadyRestoredQuantity = await productReservationManager.GetReservedByReferenceAsync(
            productId,
            orderId,
            ProductReservationReason.DeliveryNoteCancelled,
            deliveryNoteId).ConfigureAwait(false);
        var missingQuantity = targetQuantity - alreadyRestoredQuantity;
        if (missingQuantity <= 0)
        {
            return;
        }

        await productReservationManager.ReserveAsync(
            productId,
            missingQuantity,
            orderId,
            ProductReservationReason.DeliveryNoteCancelled,
            deliveryNoteId).ConfigureAwait(false);
    }

    public async Task<DeliveryNoteDto?> GetByIdAsync(Guid id)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(id).ConfigureAwait(false);
        return deliveryNote is null ? null : MapToDto(deliveryNote);
    }

    public async Task<IPagedDataDto<DeliveryNoteDto>> GetDeliveryNotesAsync(int pageIndex, int pageSize,
        string? keywords, Guid? orderId, IEnumerable<DeliveryNoteStatus>? status)
    {
        var deliverySpec = new CompositeSpecification<DeliveryNote>();

        if (!string.IsNullOrWhiteSpace(keywords))
            deliverySpec.And(new DeliveryNoteKeywordSearchSpec(keywords));

        if (orderId.HasValue)
            deliverySpec.And(new DeliveryNotesOfOrdersSpec([orderId.Value]));

        if (status != null && status.Any())
            deliverySpec.And(new HaveStatusDeliveryNoteSpec(status.ToList()));

        int? totalCount = pageIndex == 0 && pageSize == int.MaxValue
            ? null : await deliveryNoteReader.CountAsync(deliverySpec).ConfigureAwait(false);

        if (totalCount.HasValue && totalCount == 0)
            return PagedDataDto.Create(new List<DeliveryNoteDto>(), pageIndex, pageSize, 0);

        deliverySpec.ApplyOrderByDescending(deliveryNote => deliveryNote.CreatedOnUtc);
        var pagedData = await deliveryNoteReader.GetPagedListAsync(deliverySpec, pageIndex, pageSize).ConfigureAwait(false);

        return PagedDataDto.Create(pagedData.Select(MapToDto).ToList(), pageIndex, pageSize, totalCount);
    }

    public async Task<IDictionary<Guid, decimal>> GetDeliveredQuantitiesAsync(IEnumerable<Guid> orderItemIds)
    {
        var ids = orderItemIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, decimal>();

        var deliveredQuantities = await deliveryNoteReader.DataSource
            .Where(x => x.Status == DeliveryNoteStatus.Delivered)
            .SelectMany(x => x.Items)
            .Where(x => ids.Contains(x.OrderItemId))
            .GroupBy(x => x.OrderItemId)
            .Select(g => new { OrderItemId = g.Key, DeliveredQuantity = g.Sum(x => x.Quantity) })
            .ToListAsync().ConfigureAwait(false);

        IDictionary<Guid, decimal> result = deliveredQuantities.ToDictionary(x => x.OrderItemId, x => x.DeliveredQuantity);

        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
                result[id] = 0;
        }

        return result;
    }

    public async Task<IDictionary<Guid, List<DeliveryNoteLinkDto>>> GetDeliveryNoteLinksAsync(IEnumerable<Guid> orderItemIds)
    {
        var ids = orderItemIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, List<DeliveryNoteLinkDto>>();

        var links = (await deliveryNoteReader.DataSource
            .Where(x => x.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(x => x.Items.Select(i => new { i.OrderItemId, x.Id, x.Code, x.Status, x.CreatedOnUtc }))
            .Where(x => ids.Contains(x.OrderItemId))
            .ToListAsync().ConfigureAwait(false))
            .GroupBy(x => x.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new DeliveryNoteLinkDto(x.Id, x.Code, x.Status, x.CreatedOnUtc)).Distinct().ToList()
            );

        foreach (var id in ids)
        {
            if (!links.ContainsKey(id))
                links[id] = [];
        }

        return links;
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

        var customerReturn = await customerReturnManager.CreateAsync(new CreateCustomerReturnDto
        {
            DeliveryNoteId = deliveryNote.Id,
            CustomerId = deliveryNote.CustomerId,
            WarehouseId = null,
            AdditionalCost = acceptance.AgreedCustomerCharge,
            CompensateInNextDelivery = acceptance.CompensateInNextDelivery,
            Note = BuildAutoGeneratedReturnNote(deliveryNote, acceptance, rejectedLines, deliveryItemsById),
            Items = itemDtos
        }).ConfigureAwait(false);
        await customerReturnManager.MoveToInspectingAsync(customerReturn.Id).ConfigureAwait(false);
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

    private static DeliveryAcceptanceResolution ResolveDeliveryAcceptance(DeliveryNote deliveryNote, DeliveryAcceptanceDto? acceptance)
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
        decimal rejectedGoodsAmount = 0;
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
            rejectedGoodsAmount += rejectedQuantity * item.UnitPrice;
        }

        var agreedCustomerCharge = acceptance?.AgreedCustomerCharge ?? 0m;
        // Dùng AmountToCollect gốc làm cơ sở để bảo toàn OrderDiscount.
        // acceptedGoodsAmount (qty × unitPrice) không trừ discount nên không thể dùng trực tiếp.
        var amountToCollect = deliveryNote.AmountToCollect - rejectedGoodsAmount + agreedCustomerCharge;
        // debtAmount = toàn bộ nghĩa vụ khách phải trả (phần reject sẽ được bù bởi credit note).
        var debtAmount = deliveryNote.TotalAmount + deliveryNote.Surcharge + agreedCustomerCharge;
        return new DeliveryAcceptanceResolution(
            Math.Max(0m, amountToCollect),
            Math.Max(0m, debtAmount),
            rejectedGoodsAmount,
            agreedCustomerCharge,
            acceptance?.AgreedCustomerChargeReason,
            acceptance?.CompensateInNextDelivery ?? false,
            lines);
    }

    private static bool SameQuantity(decimal left, decimal right)
        => Math.Abs(left - right) <= 0.0001m;

    private async Task MarkRelatedOrderItemsReceivedByCustomerAsync(DeliveryNote deliveryNote)
    {
        var order = await orderReader.GetByIdAsync(deliveryNote.OrderId).ConfigureAwait(false);
        if (order is null)
            return;

        var deliveredQuantitiesByOrderItem = await GetNetDeliveredQuantitiesByOrderItemAsync(order.Id, deliveryNote).ConfigureAwait(false);

        foreach (var noteItem in deliveryNote.Items.Where(item => item.OrderItemId != Guid.Empty))
        {
            var orderItem = order.OrderItems.FirstOrDefault(i => i.Id == noteItem.OrderItemId);
            var deliveredQuantity = deliveredQuantitiesByOrderItem.GetValueOrDefault((SecondaryItemId)(deliveryNote.OrderId, noteItem.OrderItemId));
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

    private async Task EnsureQuantitiesCanBeDeliveredAsync(Order order,
        IReadOnlyDictionary<SecondaryItemId, decimal> requestedQuantitiesByOrderItem,
        bool includeDirectShipOutstanding = true)
    {
        var orderItemsById = order.OrderItems.ToDictionary(item => (SecondaryItemId)(order.Id, item.Id));
        var requestedOrderItemIds = requestedQuantitiesByOrderItem.Keys.ToList();
        var activeDeliveryQuantitiesByOrderItem = await GetActiveDeliveryQuantitiesByOrderItemAsync(order.Id, requestedOrderItemIds).ConfigureAwait(false);
        var directShipOutstandingQuantitiesByOrderItem = includeDirectShipOutstanding
            ? await GetDirectShipOutstandingQuantitiesByOrderItemAsync(requestedOrderItemIds).ConfigureAwait(false)
            : [];

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

    private async Task<Dictionary<SecondaryItemId, decimal>> GetActiveDeliveryQuantitiesByOrderItemAsync(Guid orderId, IReadOnlyCollection<SecondaryItemId> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return [];

        var itemIds = orderItemIds.Select(id => id.SecondaryId).ToList();
        var deliveredByOrderItem = await deliveryNoteReader.DataSource
            .Where(note => note.OrderId == orderId && note.Status != DeliveryNoteStatus.Cancelled)
            .SelectMany(note => note.Items)
            .Where(item => itemIds.Contains(item.OrderItemId))
            .GroupBy(item => item.OrderItemId)
            .ToDictionaryAsync(g => (SecondaryItemId)(orderId, g.Key), g => g.Sum(item => item.Quantity))
            .ConfigureAwait(false);

        var returnedByOrderItem = await GetReturnedQuantitiesByOrderItemAsync(orderItemIds, compensatedOnly: true).ConfigureAwait(false);
        foreach (var orderItemId in orderItemIds)
        {
            deliveredByOrderItem.TryGetValue(orderItemId, out var deliveredQuantity);
            returnedByOrderItem.TryGetValue(orderItemId, out var returnedQuantity);
            deliveredByOrderItem[orderItemId] = Math.Max(0m, deliveredQuantity - returnedQuantity);
        }

        return deliveredByOrderItem;
    }

    private async Task<Dictionary<SecondaryItemId, decimal>> GetNetDeliveredQuantitiesByOrderItemAsync(Guid orderId, DeliveryNote currentDeliveryNote)
    {
        // Phiếu đang xử lý mới chuyển Delivered ở trạng thái staged (DB vẫn còn status cũ),
        // query DB sẽ không thấy nó — loại khỏi query và cộng từ instance in-memory.
        var deliveredByOrderItems = await deliveryNoteReader.DataSource
            .Where(note => note.OrderId == orderId && note.Status == DeliveryNoteStatus.Delivered
                && note.Id != currentDeliveryNote.Id)
            .SelectMany(note => note.Items)
            .Where(item => item.OrderItemId != Guid.Empty)
            .GroupBy(item => item.OrderItemId)
            .ToDictionaryAsync(g => (SecondaryItemId)(orderId, g.Key), g => g.Sum(item => item.Quantity))
            .ConfigureAwait(false);

        if (currentDeliveryNote.Status == DeliveryNoteStatus.Delivered)
        {
            foreach (var item in currentDeliveryNote.Items.Where(item => item.OrderItemId != Guid.Empty))
            {
                deliveredByOrderItems[(SecondaryItemId)(currentDeliveryNote.OrderId, item.OrderItemId)] =
                    deliveredByOrderItems.GetValueOrDefault((SecondaryItemId)(currentDeliveryNote.OrderId, item.OrderItemId)) + item.Quantity;
            }
        }

        var orderItemIds = deliveredByOrderItems.Keys.ToList();
        var returnedByOrderItem = await GetReturnedQuantitiesByOrderItemAsync(orderItemIds, compensatedOnly: true).ConfigureAwait(false);
        foreach (var orderItemId in orderItemIds)
        {
            var deliveredQuantity = deliveredByOrderItems.GetValueOrDefault(orderItemId);
            var returnedQuantity = returnedByOrderItem.GetValueOrDefault(orderItemId);
            deliveredByOrderItems[orderItemId] = Math.Max(0m, deliveredQuantity - returnedQuantity);
        }

        return deliveredByOrderItems;
    }

    private async Task<Dictionary<SecondaryItemId, decimal>> GetReturnedQuantitiesByOrderItemAsync(IReadOnlyCollection<SecondaryItemId> orderItemIds, bool compensatedOnly)
    {
        if (orderItemIds.Count == 0)
            return orderItemIds.ToDictionary(id => id, id => 0m);

        var orderId = orderItemIds.First().PrimaryId;

        var itemIds = orderItemIds.Select(id => id.SecondaryId).ToList();
        var validDeliveryNoteItems = await deliveryNoteReader.DataSource
            .Where(note => note.OrderId == orderId)
            .SelectMany(note => note.Items)
            .Where(item => item.OrderItemId != Guid.Empty && itemIds.Contains(item.OrderItemId))
            .Select(item => new { item.Id, item.OrderItemId })
            .ToListAsync().ConfigureAwait(false);

        var returnedByOrderItems = (await customerReturnReader.DataSource
            .Where(returnNote => returnNote.Status != CustomerReturnStatus.Cancelled
                && (!compensatedOnly || (returnNote.CompensateInNextDelivery && returnNote.Status != CustomerReturnStatus.Draft)))
            .SelectMany(returnNote => returnNote.Items)
            .Where(returnItem => returnItem.DeliveryNoteItemId != null)
            .ToListAsync().ConfigureAwait(false))
            .Join(
                validDeliveryNoteItems,
                returnItem => returnItem.DeliveryNoteItemId,
                dnItem => dnItem.Id,
                (returnItem, dnItem) => new { dnItem.OrderItemId, returnItem.AcceptedQuantity }
            )
            .GroupBy(x => x.OrderItemId)
            .ToDictionary(
                group => (SecondaryItemId)(orderId, group.Key),
                group => group.Sum(x => x.AcceptedQuantity)
            );

        foreach (var orderItemId in orderItemIds)
        {
            if (!returnedByOrderItems.ContainsKey(orderItemId))
                returnedByOrderItems[orderItemId] = 0m;
        }

        return returnedByOrderItems;
    }

    private async Task<Dictionary<SecondaryItemId, decimal>> GetDirectShipOutstandingQuantitiesByOrderItemAsync(IReadOnlyCollection<SecondaryItemId> orderItemIds)
    {
        if (orderItemIds.Count == 0)
            return [];

        var itemIds = orderItemIds.Select(id => id.SecondaryId).ToList();
        return (await allocationReader.DataSource
            .Where(allocation => allocation.IsDirectShip
                && allocation.Status != AllocationStatus.Cancelled
                && itemIds.Contains(allocation.OrderItemId.SecondaryId))
            .ToListAsync().ConfigureAwait(false))
            .GroupBy(allocation => allocation.OrderItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(allocation => Math.Max(0m, allocation.AllocatedQuantity - allocation.ReceivedQuantity)));
    }

    public async Task AdminUpdateAmountToCollectAsync(Guid deliveryNoteId, decimal newAmount, string? note, Guid? adminUserId)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(deliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(deliveryNoteId);

        var run = await deliveryRunReader.DataSource
            .FirstOrDefaultAsync(r =>
                r.Status != DeliveryRunStatus.Cancelled &&
                r.Status != DeliveryRunStatus.Closed &&
                r.Items.Any(item => item.DeliveryNoteId == deliveryNoteId))
            .ConfigureAwait(false);

        if (run is null)
        {
            // check if there's a closed/cancelled run — also block in that case
            var closedRun = await deliveryRunReader.DataSource
                .FirstOrDefaultAsync(r =>
                    (r.Status == DeliveryRunStatus.Closed || r.Status == DeliveryRunStatus.Cancelled) &&
                    r.Items.Any(item => item.DeliveryNoteId == deliveryNoteId))
                .ConfigureAwait(false);
            if (closedRun is not null)
                throw new NamEcommerceDomainException("Error.DeliveryRun.CannotUpdateAmountWhenRunClosedOrCancelled");
        }

        // entity guard: status Delivered/Cancelled throws
        deliveryNote.UpdateAmountToCollect(newAmount, note);
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);

        if (run is null)
            return;

        var trackedRun = await deliveryRunRepository.GetByIdAsync(run.Id).ConfigureAwait(false);
        if (trackedRun is null)
            return;

        trackedRun.UpdateItemAmountToCollect(deliveryNoteId, newAmount);
        await deliveryRunRepository.UpdateAsync(trackedRun).ConfigureAwait(false);
    }

    private sealed record DeliveryAcceptanceLine(
        Guid DeliveryNoteItemId,
        decimal AcceptedQuantity,
        decimal RejectedQuantity,
        string? RejectReason);

    private sealed record DeliveryAcceptanceResolution(
        decimal AmountToCollect,
        decimal DebtAmount,
        decimal RejectedGoodsAmount,
        decimal AgreedCustomerCharge,
        string? AgreedCustomerChargeReason,
        bool CompensateInNextDelivery,
        IReadOnlyList<DeliveryAcceptanceLine> Lines);

    private static DeliveryNoteDto MapToDto(DeliveryNote deliveryNote)
    {
        return new DeliveryNoteDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderId = deliveryNote.OrderId,
            OrderCode = deliveryNote.OrderCode,
            AssignedDeliveryUserId = deliveryNote.AssignedDeliveryUserId,
            AssignedDeliveryUsername = deliveryNote.AssignedDeliveryUsername,
            AssignedDeliveryFullName = deliveryNote.AssignedDeliveryFullName,
            AssignedDeliveryOnUtc = deliveryNote.AssignedDeliveryOnUtc,
            CustomerId = deliveryNote.CustomerId,
            CustomerName = deliveryNote.CustomerInfo.FullName,
            CustomerPhone = deliveryNote.CustomerInfo.PhoneNumber,
            CustomerAddress = deliveryNote.CustomerInfo.Address,
            ShippingAddress = deliveryNote.ShippingAddress,
            ShippingPhoneNumber = deliveryNote.ShippingPhoneNumber,
            CanUpdateShippingInfo = deliveryNote.CanEditShippingInfo(),
            ShowPrice = deliveryNote.ShowPrice,
            Note = deliveryNote.Note,
            Status = deliveryNote.Status,
            SourceType = deliveryNote.SourceType,
            IsDirectShip = deliveryNote.IsDirectShip,
            DeliveryConfirmationStatus = deliveryNote.DeliveryConfirmationStatus,
            ConfirmedAtUtc = deliveryNote.ConfirmedAtUtc,
            ConfirmedNote = deliveryNote.ConfirmedNote,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            DeliveryProofPictureId = deliveryNote.DeliveryProofPictureId,
            DeliveryReceiverName = deliveryNote.DeliveryReceiverName,
            DeliveryLatitude = deliveryNote.DeliveryLatitude,
            DeliveryLongitude = deliveryNote.DeliveryLongitude,
            DeliveryLocationAddress = deliveryNote.DeliveryLocationAddress,
            DeliveryCompletionNote = deliveryNote.DeliveryCompletionNote,
            DeliveryCompletionSource = deliveryNote.DeliveryCompletionSource,
            DeliveryCompletionIdempotencyKey = deliveryNote.DeliveryCompletionIdempotencyKey,
            DeliveryCashCollectedAmount = deliveryNote.DeliveryCashCollectedAmount,
            CreatedByUserId = deliveryNote.CreatedByUserId,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            UpdatedOnUtc = deliveryNote.UpdatedOnUtc,
            TotalAmount = deliveryNote.TotalAmount,
            Surcharge = deliveryNote.Surcharge,
            AppliedOrderDiscount = deliveryNote.AppliedOrderDiscount,
            AppliedOrderPrepaid = deliveryNote.AppliedOrderPrepaid,
            SurchargeReason = deliveryNote.SurchargeReason,
            AmountToCollect = deliveryNote.AmountToCollect,
            AmountToCollectOverriddenAt = deliveryNote.AmountToCollectOverriddenAt,
            AmountToCollectOverrideNote = deliveryNote.AmountToCollectOverrideNote,
            SettlementApproval = deliveryNote.SettlementApproval,
            ProposedAmountToCollect = deliveryNote.ProposedAmountToCollect,
            ApprovedAmountToCollect = deliveryNote.ApprovedAmountToCollect,
            ApprovedAgreedCustomerCharge = deliveryNote.ApprovedAgreedCustomerCharge,
            ApprovedAgreedChargeReason = deliveryNote.ApprovedAgreedChargeReason,
            SettlementReason = deliveryNote.SettlementReason,
            SettlementAdminNote = deliveryNote.SettlementAdminNote,
            SettlementItems = deliveryNote.SettlementItems.Select(s => new DeliveryNoteSettlementItemDto
            {
                DeliveryNoteItemId = s.DeliveryNoteItemId,
                AcceptedQuantity = s.AcceptedQuantity,
                RejectedQuantity = s.RejectedQuantity,
                RejectReason = s.RejectReason
            }).ToList(),
            Items = deliveryNote.Items.Select(i => new DeliveryNoteItemDto
            {
                Id = i.Id,
                DeliveryNoteId = i.DeliveryNoteId,
                OrderItemId = i.OrderItemId,
                ProductId = i.ProductId,
                WarehouseId = i.WarehouseId,
                ProductName = i.ProductName ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                CostAtDispatch = i.CostAtDispatch
            }).ToList(),
            RequiresPaymentToConfirm = deliveryNote.RequiresPaymentToConfirm,
            HasPaid = deliveryNote.HasPaid(),
            CanApprove = deliveryNote.CanApprove(),
            CanReject = deliveryNote.CanReject(),
            CanMarkDelivering = deliveryNote.CanMarkDelivering(),
            CanMarkDelivered = deliveryNote.CanMarkDelivered(),
            CanProcess = deliveryNote.CanProcess()
        };
    }

    public async Task MarkAsOrderIsPaid(Guid deliveryNoteId)
    {
        var deliveryNote = await deliveryNoteRepository.GetByIdAsync(deliveryNoteId);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(deliveryNoteId);

        if (!deliveryNote.RequiresPaymentToConfirm || deliveryNote.Status != DeliveryNoteStatus.Draft || deliveryNote.PaidOnUtc.HasValue)
            throw new DeliveryNoteCannotMarkedAsPaidException();

        deliveryNote.MarkIsPaid();
        await deliveryNoteRepository.UpdateAsync(deliveryNote).ConfigureAwait(false);
    }

    public async Task<Guid?> GetWaitingPaymentDeliveryNoteIdAsync(Guid orderId)
    {
        var deliveryNote = await deliveryNoteReader.DataSource
            .Where(deliveryNote => deliveryNote.OrderId == orderId
                && deliveryNote.Status == DeliveryNoteStatus.Draft
                && deliveryNote.RequiresPaymentToConfirm && deliveryNote.PaidOnUtc == null)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        return deliveryNote?.Id;
    }
}
