using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Domain.Services.Returns;

public sealed class CustomerReturnManager(
    IRepository<CustomerReturn> customerReturnRepository,
    IEntityDataReader<CustomerReturn> customerReturnDataReader,
    IEntityDataReader<Order> orderDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IEntityDataReader<CustomerDebt> customerDebtDataReader,
    IRepository<CustomerDebt> customerDebtRepository) : ICustomerReturnManager
{
    public async Task<CustomerReturnDto> CreateAsync(CreateCustomerReturnDto dto, Guid? createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var order = await orderDataReader.GetByIdAsync(dto.OrderId).ConfigureAwait(false);
        if (order is null)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.OrderNotFound", dto.OrderId);

        var warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.WarehouseNotFound", dto.WarehouseId);

        var code = GenerateCode();

        var customerReturn = new CustomerReturn(
            code: code,
            orderId: order.Id,
            orderCode: order.Code,
            customerId: order.CustomerId,
            customerName: order.CustomerName ?? string.Empty,
            warehouseId: warehouse.Id,
            warehouseName: warehouse.Name,
            note: dto.Note,
            createdByUserId: createdByUserId);

        foreach (var itemDto in dto.Items)
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
                unitPrice: itemDto.UnitPrice);
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
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(id);

        // Validate: tổng AcceptedQuantity không được vượt quá số đã giao theo (OrderId, ProductId)
        foreach (var item in customerReturn.Items)
        {
            var deliveredQty = GetTotalDeliveredQuantity(customerReturn.OrderId, item.ProductId);
            var previouslyReturned = await GetTotalConfirmedReturnQuantityAsync(
                customerReturn.OrderId, item.ProductId, excludeReturnId: id).ConfigureAwait(false);
            var maxAllowed = deliveredQty - previouslyReturned;

            if (item.AcceptedQuantity > maxAllowed)
                throw new ExceedsDeliveredQuantityException(item.ProductId, item.AcceptedQuantity, maxAllowed);
        }

        customerReturn.Confirm();
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    public async Task CancelAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(id);

        customerReturn.Cancel();
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    public async Task<CustomerReturnDto?> GetByIdAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return customerReturn?.ToDto();
    }

    public Task<(int Total, List<CustomerReturnDto> Items)> GetListAsync(
        Guid? customerId, Guid? orderId, int? status, int pageIndex, int pageSize)
    {
        var query = customerReturnDataReader.DataSource;

        if (customerId.HasValue)
            query = query.Where(r => r.CustomerId == customerId.Value);
        if (orderId.HasValue)
            query = query.Where(r => r.OrderId == orderId.Value);
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
        Guid orderId, Guid productId, Guid? excludeReturnId = null)
    {
        var query = customerReturnDataReader.DataSource
            .Where(r => r.OrderId == orderId
                        && (int)r.Status == 2 // Confirmed
                        && (excludeReturnId == null || r.Id != excludeReturnId));

        // Tính tổng qua Items — EF sẽ flatten collection
        decimal total = 0;
        var confirmedReturns = query.ToList();
        foreach (var ret in confirmedReturns)
            total += ret.Items
                .Where(i => i.ProductId == productId)
                .Sum(i => i.AcceptedQuantity);

        return Task.FromResult(total);
    }

    public async Task FinalizeConfirmAsync(Guid returnId, Guid generatedGoodsReceiptId, decimal totalReturnAmount)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(returnId).ConfigureAwait(false);
        if (customerReturn is null) return;

        // Idempotency guard — đã xử lý rồi thì không làm lại
        if (customerReturn.GeneratedGoodsReceiptId.HasValue) return;

        customerReturn.GeneratedGoodsReceiptId = generatedGoodsReceiptId;
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);

        if (totalReturnAmount <= 0) return;

        // Giảm CustomerDebt FIFO theo CreatedOnUtc, chỉ lấy debt của Order này
        var orderedDebts = customerDebtDataReader.DataSource
            .Where(d => d.OrderId == customerReturn.OrderId)
            .OrderBy(d => d.CreatedOnUtc)
            .ToList();

        if (!orderedDebts.Any()) return;

        var touchedDebts = new List<CustomerDebt>();
        var remaining = totalReturnAmount;

        foreach (var debt in orderedDebts)
        {
            if (remaining <= 0) break;
            if (debt.RemainingAmount <= 0) continue;

            var toApply = Math.Min(remaining, debt.RemainingAmount);
            debt.ApplyReturn(toApply, returnId);
            touchedDebts.Add(debt);
            remaining -= toApply;
        }

        // Tổng trả > tổng nợ → áp phần thừa vào debt cũ nhất (cho phép âm)
        if (remaining > 0)
        {
            var first = orderedDebts[0];
            first.ApplyReturn(remaining, returnId);
            if (!touchedDebts.Contains(first))
                touchedDebts.Add(first);
        }

        foreach (var debt in touchedDebts)
            await customerDebtRepository.UpdateAsync(debt).ConfigureAwait(false);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private string GenerateCode()
    {
        var datePrefix = $"TKH-{DateTime.UtcNow:yyyyMMdd}";
        var count = customerReturnDataReader.DataSource.Count(r => r.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
    }

    private decimal GetTotalDeliveredQuantity(Guid orderId, Guid productId)
    {
        // Join DeliveryNote (Delivered status = 3) + DeliveryNoteItem theo orderId + productId
        var deliveredNotes = deliveryNoteDataReader.DataSource
            .Where(dn => dn.OrderId == orderId && dn.Status == DeliveryNoteStatus.Delivered) // Delivered
            .ToList();

        if (!deliveredNotes.Any())
            return 0;

        return deliveredNotes
            .SelectMany(dn => dn.Items.Where(item => item.ProductId == productId))
            .Where(item => item.ProductId == productId)
            .Sum(item => item.Quantity);
    }
}
