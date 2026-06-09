using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Returns;

public sealed class CustomerReturnManager(
    IRepository<CustomerReturn> customerReturnRepository,
    IEntityDataReader<CustomerReturn> customerReturnDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IEntityDataReader<CustomerReturnRequest> customerReturnRequestDataReader,
    ICustomerDebtManager customerDebtManager,
    IExpenseManager expenseManager,
    IProductReservationManager productReservationManager,
    ICurrentUserAccessor currentUserAccessor) : ICustomerReturnManager
{
    public async Task<CustomerReturnDto> CreateAsync(CreateCustomerReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();
        var itemDtos = dto.Items.ToList();

        Warehouse? warehouse = null;
        if (dto.WarehouseId.HasValue)
        {
            warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.WarehouseNotFound", dto.WarehouseId.Value);
        }

        var deliveryNotes = await GetDeliveredNotesForReturnAsync(dto.CustomerId, dto.DeliveryNoteId)
            .ConfigureAwait(false);
        if (deliveryNotes.Count == 0)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.NoDeliveredItems");

        var sourceItemsById = deliveryNotes
            .SelectMany(dn => dn.Items.Select(item => new ReturnSourceItem(dn, item)))
            .ToDictionary(source => source.Item.Id);
        var normalizedItems = new List<CreateCustomerReturnItemDto>();
        var sourceDeliveryNotesById = new Dictionary<Guid, DeliveryNote>();
        var localReservedBySourceItemId = new Dictionary<Guid, decimal>();

        foreach (var itemDto in itemDtos)
        {
            if (itemDto.DeliveryNoteItemId.HasValue)
            {
                var source = ValidateSourceItem(
                    sourceItemsById,
                    itemDto.DeliveryNoteItemId.Value,
                    itemDto.ProductId,
                    dto.DeliveryNoteId);

                var reservedReturnQty = await GetTotalReservedReturnQuantityForDeliveryNoteItemAsync(
                    source.DeliveryNote.Id,
                    source.Item.Id,
                    itemDto.ProductId,
                    excludeReturnRequestId: dto.ExcludeCustomerReturnRequestId).ConfigureAwait(false);
                localReservedBySourceItemId.TryGetValue(source.Item.Id, out var localReservedQty);
                var maxAllowed = source.Item.Quantity - reservedReturnQty - localReservedQty;
                if (itemDto.AcceptedQuantity > maxAllowed)
                    throw new ExceedsDeliveredQuantityException(itemDto.ProductId, itemDto.AcceptedQuantity, Math.Max(0m, maxAllowed));

                normalizedItems.Add(itemDto with
                {
                    OriginalUnitPrice = itemDto.OriginalUnitPrice ?? source.Item.UnitPrice
                });
                localReservedBySourceItemId[source.Item.Id] = localReservedQty + itemDto.AcceptedQuantity;
                sourceDeliveryNotesById.TryAdd(source.DeliveryNote.Id, source.DeliveryNote);
                continue;
            }

            var allocatedItems = await AllocateProductReturnAsync(
                dto.CustomerId,
                dto.DeliveryNoteId,
                itemDto,
                deliveryNotes,
                localReservedBySourceItemId,
                dto.ExcludeCustomerReturnRequestId).ConfigureAwait(false);
            foreach (var allocatedItem in allocatedItems)
            {
                normalizedItems.Add(allocatedItem);
                var source = sourceItemsById[allocatedItem.DeliveryNoteItemId!.Value];
                sourceDeliveryNotesById.TryAdd(source.DeliveryNote.Id, source.DeliveryNote);
            }
        }

        if (normalizedItems.Count == 0)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.NoItems");

        var deliveryNote = dto.DeliveryNoteId.HasValue
            ? deliveryNotes.First(dn => dn.Id == dto.DeliveryNoteId.Value)
            : sourceDeliveryNotesById.Values
                .OrderBy(dn => dn.DeliveredOnUtc ?? dn.CreatedOnUtc)
                .First();

        var customerId = dto.CustomerId;
        var deliveryCustomerName = deliveryNote.CustomerInfo.FullName.ToString();
        var customerName = string.IsNullOrWhiteSpace(deliveryCustomerName)
            ? "Khách hàng"
            : deliveryCustomerName;

        var code = GenerateCode();
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);

        var customerReturn = new CustomerReturn(
            code: code,
            deliveryNoteId: deliveryNote.Id,
            deliveryNoteCode: deliveryNote.Code,
            customerId: customerId,
            customerName: customerName,
            warehouseId: warehouse?.Id,
            warehouseName: warehouse?.Name,
            note: dto.Note,
            additionalCost: dto.AdditionalCost,
            compensateInNextDelivery: dto.CompensateInNextDelivery,
            createdByUserId: currentUser?.Id);

        foreach (var itemDto in normalizedItems)
        {
            var product = await productDataReader.GetByIdAsync(itemDto.ProductId).ConfigureAwait(false);
            if (product is null)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.ProductNotFound", itemDto.ProductId);

            customerReturn.AddItem(
                productId: itemDto.ProductId,
                productName: product.Name,
                deliveryNoteItemId: itemDto.DeliveryNoteItemId,
                requestedQuantity: itemDto.RequestedQuantity,
                acceptedQuantity: itemDto.AcceptedQuantity,
                originalUnitPrice: itemDto.OriginalUnitPrice,
                returnUnitPrice: itemDto.ReturnUnitPrice);
        }

        customerReturn.MarkCreated();
        var inserted = await customerReturnRepository.InsertAsync(customerReturn).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task<CustomerReturnDto> UpdateAsync(UpdateCustomerReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var customerReturn = await customerReturnDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(dto.Id);

        customerReturn.Note = dto.Note;
        if (dto.ReturnDate.HasValue)
            customerReturn.ReturnDate = dto.ReturnDate.Value;

        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
        return customerReturn.ToDto();
    }

    public async Task MoveToInspectingAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(id);

        customerReturn.MoveToInspecting();
        await ReserveCompensatedQuantityAsync(customerReturn).ConfigureAwait(false);
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id, Guid? warehouseId = null)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(id);

        if (warehouseId.HasValue)
        {
            if (warehouseId.Value == Guid.Empty)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.WarehouseRequired");

            var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId.Value).ConfigureAwait(false);
            if (warehouse is null)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.WarehouseNotFound", warehouseId.Value);

            customerReturn.SetWarehouse(warehouse.Id, warehouse.Name);
        }

        // Validate lại theo các hàng đã giao của khách. Phiếu giao trên CustomerReturn chỉ là tham chiếu nội bộ;
        // từng dòng có thể được hệ thống tự phân bổ từ nhiều phiếu giao của cùng khách.
        var deliveryNotes = await GetDeliveredNotesForReturnAsync(customerReturn.CustomerId, null)
            .ConfigureAwait(false);
        var deliveryNoteItemsById = deliveryNotes
            .SelectMany(dn => dn.Items.Select(item => new ReturnSourceItem(dn, item)))
            .ToDictionary(source => source.Item.Id);
        var acceptedByDeliveryItem = customerReturn.Items
            .Where(item => item.DeliveryNoteItemId.HasValue)
            .GroupBy(item => item.DeliveryNoteItemId!.Value)
            .Select(group => new
            {
                DeliveryNoteItemId = group.Key,
                ProductId = group.First().ProductId,
                AcceptedQuantity = group.Sum(item => item.AcceptedQuantity)
            });
        foreach (var item in acceptedByDeliveryItem)
        {
            if (!deliveryNoteItemsById.TryGetValue(item.DeliveryNoteItemId, out var source) ||
                source.Item.ProductId != item.ProductId)
            {
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.DeliveryNoteItemRequired", item.ProductId);
            }

            var previouslyReturned = await GetTotalReservedReturnQuantityForDeliveryNoteItemAsync(
                source.DeliveryNote.Id,
                item.DeliveryNoteItemId,
                item.ProductId,
                excludeReturnId: id).ConfigureAwait(false);
            var maxAllowed = source.Item.Quantity - previouslyReturned;

            if (item.AcceptedQuantity > maxAllowed)
                throw new ExceedsDeliveredQuantityException(item.ProductId, item.AcceptedQuantity, Math.Max(0m, maxAllowed));
        }

        var acceptedWithoutDeliveryItemByProduct = customerReturn.Items
            .Where(item => !item.DeliveryNoteItemId.HasValue)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                AcceptedQuantity = group.Sum(item => item.AcceptedQuantity)
        });
        foreach (var item in acceptedWithoutDeliveryItemByProduct)
        {
            var deliveredQty = GetTotalDeliveredQuantity(customerReturn.CustomerId, item.ProductId);
            var previouslyReturned = await GetTotalReservedReturnQuantityForCustomerProductAsync(
                customerReturn.CustomerId, item.ProductId, excludeReturnId: id).ConfigureAwait(false);
            var maxAllowed = deliveredQty - previouslyReturned;

            if (item.AcceptedQuantity > maxAllowed)
                throw new ExceedsDeliveredQuantityException(item.ProductId, item.AcceptedQuantity, Math.Max(0m, maxAllowed));
        }

        customerReturn.Confirm();
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(id);

        await ReleaseCompensatedReservationAsync(customerReturn).ConfigureAwait(false);
        customerReturn.Cancel();
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    private async Task ReserveCompensatedQuantityAsync(CustomerReturn customerReturn)
    {
        if (!customerReturn.CompensateInNextDelivery)
            return;

        var quantities = GetCompensatedQuantitiesByOrderProduct(customerReturn);
        foreach (var item in quantities)
        {
            var alreadyReserved = await productReservationManager.GetReservedByReferenceAsync(
                item.ProductId,
                item.OrderId,
                ProductReservationReason.CustomerReturnCompensation,
                customerReturn.Id).ConfigureAwait(false);

            var quantityToReserve = item.Quantity - alreadyReserved;
            if (quantityToReserve <= 0)
                continue;

            await productReservationManager.ReserveAsync(
                item.ProductId,
                quantityToReserve,
                item.OrderId,
                ProductReservationReason.CustomerReturnCompensation,
                customerReturn.Id).ConfigureAwait(false);
        }
    }

    private async Task ReleaseCompensatedReservationAsync(CustomerReturn customerReturn)
    {
        if (!customerReturn.CompensateInNextDelivery)
            return;

        var quantities = GetCompensatedQuantitiesByOrderProduct(customerReturn);
        foreach (var item in quantities)
        {
            var reservedByReturn = await productReservationManager.GetReservedByReferenceAsync(
                item.ProductId,
                item.OrderId,
                ProductReservationReason.CustomerReturnCompensation,
                customerReturn.Id).ConfigureAwait(false);
            if (reservedByReturn <= 0)
                continue;

            var reservedForOrder = await productReservationManager.GetReservedForOrderAsync(
                item.ProductId,
                item.OrderId).ConfigureAwait(false);
            var quantityToRelease = Math.Min(reservedByReturn, reservedForOrder);
            if (quantityToRelease <= 0)
                continue;

            await productReservationManager.ReleaseAsync(
                item.ProductId,
                quantityToRelease,
                item.OrderId,
                ProductReservationReason.CustomerReturnCompensation,
                customerReturn.Id).ConfigureAwait(false);
        }
    }

    private List<CompensatedQuantity> GetCompensatedQuantitiesByOrderProduct(CustomerReturn customerReturn)
    {
        var sourceItemsById = deliveryNoteDataReader.DataSource
            .Where(note => note.CustomerId == customerReturn.CustomerId && note.OrderId != Guid.Empty)
            .SelectMany(note => note.Items.Select(item => new
            {
                note.OrderId,
                item.Id,
                item.ProductId,
                item.OrderItemId
            }))
            .ToDictionary(item => item.Id);

        if (sourceItemsById.Count == 0)
            return [];

        return customerReturn.Items
            .Where(item => item.AcceptedQuantity > 0 && item.DeliveryNoteItemId.HasValue)
            .Select(item => new
            {
                ReturnItem = item,
                SourceItem = sourceItemsById.GetValueOrDefault(item.DeliveryNoteItemId!.Value)
            })
            .Where(item => item.SourceItem is not null
                && item.SourceItem.ProductId == item.ReturnItem.ProductId
                && item.SourceItem.OrderItemId != Guid.Empty)
            .GroupBy(item => new { item.SourceItem!.OrderId, item.ReturnItem.ProductId })
            .Select(group => new CompensatedQuantity(
                group.Key.OrderId,
                group.Key.ProductId,
                group.Sum(item => item.ReturnItem.AcceptedQuantity)))
            .ToList();
    }

    public async Task<CustomerReturnDto?> GetByIdAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return customerReturn?.ToDto();
    }

    public Task<(int Total, List<CustomerReturnDto> Items)> GetListAsync(
        Guid? customerId, Guid? deliveryNoteId, int? status, int pageIndex, int pageSize)
    {
        var query = customerReturnDataReader.DataSource;

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);
        if (deliveryNoteId.HasValue)
            query = query.Where(r => r.DeliveryNoteId == deliveryNoteId.Value);
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

    public Task<decimal> GetTotalReservedReturnQuantityAsync(
        Guid deliveryNoteId, Guid productId, Guid? excludeReturnId = null)
    {
        var query = customerReturnDataReader.DataSource
            .Where(r => r.DeliveryNoteId == deliveryNoteId
                        && r.Status != CustomerReturnStatus.Cancelled
                        && (excludeReturnId == null || r.Id != excludeReturnId));

        decimal total = 0;
        var reservedReturns = query.ToList();
        foreach (var ret in reservedReturns)
            total += ret.Items
                .Where(i => i.ProductId == productId)
                .Sum(i => i.AcceptedQuantity);

        total += customerReturnRequestDataReader.DataSource
            .Where(request => request.DeliveryNoteId == deliveryNoteId
                && (request.Status == CustomerReturnRequestStatus.PendingReview ||
                    request.Status == CustomerReturnRequestStatus.Accepted))
            .SelectMany(request => request.Items.Where(item => item.ProductId == productId))
            .Sum(item => item.RequestedQuantity);

        return Task.FromResult(total);
    }

    private Task<decimal> GetTotalReservedReturnQuantityForDeliveryNoteItemAsync(
        Guid deliveryNoteId,
        Guid deliveryNoteItemId,
        Guid productId,
        Guid? excludeReturnId = null,
        Guid? excludeReturnRequestId = null)
    {
        var query = customerReturnDataReader.DataSource
            .Where(r => r.Status != CustomerReturnStatus.Cancelled
                        && (excludeReturnId == null || r.Id != excludeReturnId));

        decimal total = 0;
        var reservedReturns = query.ToList();
        foreach (var ret in reservedReturns)
            total += ret.Items
                .Where(i => i.DeliveryNoteItemId == deliveryNoteItemId ||
                            (!i.DeliveryNoteItemId.HasValue && ret.DeliveryNoteId == deliveryNoteId && i.ProductId == productId))
                .Sum(i => i.AcceptedQuantity);

        total += customerReturnRequestDataReader.DataSource
            .Where(request => request.Status == CustomerReturnRequestStatus.PendingReview ||
                              request.Status == CustomerReturnRequestStatus.Accepted)
            .Where(request => excludeReturnRequestId == null || request.Id != excludeReturnRequestId.Value)
            .SelectMany(request => request.Items.Where(item => item.DeliveryNoteItemId == deliveryNoteItemId))
            .Sum(item => item.RequestedQuantity);

        return Task.FromResult(total);
    }

    /// <summary>
    /// Hoàn tất hiệu ứng tài chính sau khi CustomerReturn đã sinh GoodsReceipt.
    /// Khoản hoàn được cấn trừ theo FIFO trên toàn bộ nợ của customer, không giới hạn trong DeliveryNote gốc.
    /// Đây là chủ ý nghiệp vụ: tiền trả hàng làm giảm công nợ khách hàng theo tuổi nợ, phần vượt nợ sẽ sinh CustomerRefund.
    /// </summary>
    public async Task FinalizeConfirmAsync(Guid returnId, Guid generatedGoodsReceiptId, decimal netRefundAmount)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(returnId).ConfigureAwait(false);
        if (customerReturn is null) return;

        // Idempotency guard — đã xử lý rồi thì không làm lại
        if (customerReturn.GeneratedGoodsReceiptId.HasValue) return;

        customerReturn.GeneratedGoodsReceiptId = generatedGoodsReceiptId;
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);

        if (customerReturn.AdditionalCost > 0)
        {
            await expenseManager.CreateExpenseAsync(new CreateExpenseDto
            {
                Title = $"Chi phí phát sinh phiếu trả hàng {customerReturn.Code}",
                Description = $"Chi phí phát sinh khi nhận hàng trả từ khách {customerReturn.CustomerName}",
                Amount = customerReturn.AdditionalCost,
                ExpenseType = ExpenseType.ReturnCost,
                IncurredDate = customerReturn.ConfirmedOnUtc ?? DateTime.UtcNow,
                SourceCustomerReturnId = customerReturn.Id
            }).ConfigureAwait(false);
        }

        if (netRefundAmount <= 0) return;

        var creditNote = await customerDebtManager.ApplyCreditNoteFromCustomerReturnAsync(
            customerReturn.CustomerId,
            customerReturn.Id,
            customerReturn.Code,
            customerReturn.DeliveryNoteId == Guid.Empty ? null : customerReturn.DeliveryNoteId,
            netRefundAmount).ConfigureAwait(false);

        if (creditNote.RemainingAmount > 0)
        {
            customerReturn.MarkOverRefunded(creditNote.RemainingAmount, null);
            await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private string GenerateCode()
    {
        var datePrefix = $"TKH-{DateTime.UtcNow:yyyyMMdd}";
        var count = customerReturnDataReader.DataSource.Count(r => r.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
    }

    private async Task<List<DeliveryNote>> GetDeliveredNotesForReturnAsync(Guid customerId, Guid? deliveryNoteId)
    {
        if (customerId == Guid.Empty)
            return [];

        if (deliveryNoteId.HasValue)
        {
            var deliveryNote = await deliveryNoteDataReader.GetByIdAsync(deliveryNoteId.Value).ConfigureAwait(false);
            if (deliveryNote is null)
                throw new DeliveryNoteNotFoundException(deliveryNoteId.Value);
            if (deliveryNote.CustomerId != customerId)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.DeliveryNoteNotOwnedByCustomer", deliveryNote.Code);
            //*TODO*
            //if (deliveryNote.Status != DeliveryNoteStatus.Delivered)
            //    throw new ReturnDataIsInvalidException("Error.CustomerReturn.DeliveryNoteMustBeDelivered", deliveryNote.Code);
            if (deliveryNote.SourceType != DeliveryNoteSourceType.ToCustomer &&
                deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
                throw new ReturnDataIsInvalidException("Error.CustomerReturn.DeliveryNoteMustBeDelivered", deliveryNote.Code);

            return [deliveryNote];
        }

        return deliveryNoteDataReader.DataSource
            .Where(dn => dn.CustomerId == customerId
                         && (dn.SourceType == DeliveryNoteSourceType.ToCustomer ||
                             dn.SourceType == DeliveryNoteSourceType.DirectShipToCustomer)
                         && dn.Status == DeliveryNoteStatus.Delivered)
            .OrderBy(dn => dn.DeliveredOnUtc ?? dn.CreatedOnUtc)
            .ToList();
    }

    private static ReturnSourceItem ValidateSourceItem(
        IReadOnlyDictionary<Guid, ReturnSourceItem> sourceItemsById,
        Guid deliveryNoteItemId,
        Guid productId,
        Guid? selectedDeliveryNoteId)
    {
        if (!sourceItemsById.TryGetValue(deliveryNoteItemId, out var source) ||
            source.Item.ProductId != productId ||
            (selectedDeliveryNoteId.HasValue && source.DeliveryNote.Id != selectedDeliveryNoteId.Value))
        {
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.DeliveryNoteItemRequired", productId);
        }

        return source;
    }

    private async Task<List<CreateCustomerReturnItemDto>> AllocateProductReturnAsync(
        Guid customerId,
        Guid? selectedDeliveryNoteId,
        CreateCustomerReturnItemDto itemDto,
        IReadOnlyCollection<DeliveryNote> deliveryNotes,
        IDictionary<Guid, decimal> localReservedBySourceItemId,
        Guid? excludeReturnRequestId)
    {
        var remainingQuantity = itemDto.AcceptedQuantity;
        if (remainingQuantity <= 0)
            return [];

        var productSources = deliveryNotes
            .SelectMany(dn => dn.Items
                .Where(item => item.ProductId == itemDto.ProductId)
                .Select(item => new ReturnSourceItem(dn, item)))
            .OrderBy(source => source.DeliveryNote.DeliveredOnUtc ?? source.DeliveryNote.CreatedOnUtc)
            .ToList();

        if (productSources.Count == 0)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.ProductNotDelivered", itemDto.ProductId);

        var allocatedItems = new List<CreateCustomerReturnItemDto>();
        foreach (var source in productSources)
        {
            var reservedReturnQty = await GetTotalReservedReturnQuantityForDeliveryNoteItemAsync(
                source.DeliveryNote.Id,
                source.Item.Id,
                itemDto.ProductId,
                excludeReturnRequestId: excludeReturnRequestId).ConfigureAwait(false);
            localReservedBySourceItemId.TryGetValue(source.Item.Id, out var localReservedQty);
            var availableQuantity = Math.Max(0m, source.Item.Quantity - reservedReturnQty - localReservedQty);
            if (availableQuantity <= 0)
                continue;

            var allocatedQuantity = Math.Min(remainingQuantity, availableQuantity);
            allocatedItems.Add(itemDto with
            {
                DeliveryNoteItemId = source.Item.Id,
                RequestedQuantity = allocatedQuantity,
                AcceptedQuantity = allocatedQuantity,
                OriginalUnitPrice = itemDto.OriginalUnitPrice ?? source.Item.UnitPrice
            });

            remainingQuantity -= allocatedQuantity;
            localReservedBySourceItemId[source.Item.Id] = localReservedQty + allocatedQuantity;
            if (remainingQuantity <= 0)
                break;
        }

        if (remainingQuantity > 0)
        {
            var requestedQuantity = itemDto.AcceptedQuantity - remainingQuantity;
            var maxAllowed = Math.Max(0m, requestedQuantity);
            throw new ExceedsDeliveredQuantityException(itemDto.ProductId, itemDto.AcceptedQuantity, maxAllowed);
        }

        return allocatedItems;
    }

    private Task<decimal> GetTotalReservedReturnQuantityForCustomerProductAsync(
        Guid customerId,
        Guid productId,
        Guid? excludeReturnId = null)
    {
        var total = customerReturnDataReader.DataSource
            .Where(r => r.CustomerId == customerId
                        && r.Status != CustomerReturnStatus.Cancelled
                        && (excludeReturnId == null || r.Id != excludeReturnId))
            .ToList()
            .SelectMany(r => r.Items.Where(item => item.ProductId == productId))
            .Sum(item => item.AcceptedQuantity);

        total += customerReturnRequestDataReader.DataSource
            .Where(request => request.CustomerId == customerId
                && (request.Status == CustomerReturnRequestStatus.PendingReview ||
                    request.Status == CustomerReturnRequestStatus.Accepted))
            .SelectMany(request => request.Items.Where(item => item.ProductId == productId))
            .Sum(item => item.RequestedQuantity);

        return Task.FromResult(total);
    }

    private decimal GetTotalDeliveredQuantity(Guid customerId, Guid productId)
    {
        return deliveryNoteDataReader.DataSource
            .Where(dn => dn.CustomerId == customerId
                         && (dn.SourceType == DeliveryNoteSourceType.ToCustomer ||
                             dn.SourceType == DeliveryNoteSourceType.DirectShipToCustomer)
                         && dn.Status == DeliveryNoteStatus.Delivered)
            .SelectMany(dn => dn.Items)
            .Where(item => item.ProductId == productId)
            .Sum(item => item.Quantity);
    }

    private sealed record ReturnSourceItem(DeliveryNote DeliveryNote, DeliveryNoteItem Item);

    private sealed record CompensatedQuantity(Guid OrderId, Guid ProductId, decimal Quantity);
}
