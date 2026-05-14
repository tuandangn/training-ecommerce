using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Services.Returns;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Returns;

public sealed class CustomerReturnManager(
    IRepository<CustomerReturn> customerReturnRepository,
    IEntityDataReader<CustomerReturn> customerReturnDataReader,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IEntityDataReader<CustomerDebt> customerDebtDataReader,
    IRepository<CustomerDebt> customerDebtRepository,
    IExpenseManager expenseManager,
    ICurrentUserAccessor currentUserAccessor) : ICustomerReturnManager
{
    public async Task<CustomerReturnDto> CreateAsync(CreateCustomerReturnDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var warehouse = await warehouseDataReader.GetByIdAsync(dto.WarehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new ReturnDataIsInvalidException("Error.CustomerReturn.WarehouseNotFound", dto.WarehouseId);

        var deliveryNote = await deliveryNoteDataReader.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        var customerId = deliveryNote.CustomerId;
        var customerName = deliveryNote.CustomerName ?? string.Empty;

        var code = GenerateCode();
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);

        var customerReturn = new CustomerReturn(
            code: code,
            deliveryNoteId: deliveryNote.Id,
            deliveryNoteCode: deliveryNote.Code,
            customerId: customerId,
            customerName: customerName,
            warehouseId: warehouse.Id,
            warehouseName: warehouse.Name,
            note: dto.Note,
            additionalCost: dto.AdditionalCost,
            createdByUserId: currentUser?.Id);

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
        await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
    }

    public async Task ConfirmAsync(Guid id)
    {
        var customerReturn = await customerReturnDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new CustomerReturnNotFoundException(id);

        // CustomerReturn luôn gắn DeliveryNote — validate qty không vượt quá số đã giao trừ phần đã trả/đang trả
        foreach (var item in customerReturn.Items)
        {
            var deliveredQty = GetTotalDeliveredQuantity(customerReturn.DeliveryNoteId, item.ProductId);
            var previouslyReturned = await GetTotalReservedReturnQuantityAsync(
                customerReturn.DeliveryNoteId, item.ProductId, excludeReturnId: id).ConfigureAwait(false);
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
                        && (r.Status == CustomerReturnStatus.Inspecting || r.Status == CustomerReturnStatus.Confirmed)
                        && (excludeReturnId == null || r.Id != excludeReturnId));

        decimal total = 0;
        var reservedReturns = query.ToList();
        foreach (var ret in reservedReturns)
            total += ret.Items
                .Where(i => i.ProductId == productId)
                .Sum(i => i.AcceptedQuantity);

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

        var orderedDebts = customerDebtDataReader.DataSource
            .Where(d => d.CustomerId == customerReturn.CustomerId)
            .OrderBy(d => d.CreatedOnUtc)
            .ToList();

        if (!orderedDebts.Any()) return;

        var touchedDebts = new List<CustomerDebt>();
        var remaining = netRefundAmount;

        foreach (var debt in orderedDebts)
        {
            if (remaining <= 0) break;
            if (debt.RemainingAmount <= 0) continue;

            var toApply = Math.Min(remaining, debt.RemainingAmount);
            debt.ApplyReturn(toApply, returnId);
            touchedDebts.Add(debt);
            remaining -= toApply;
        }

        // Tổng trả > tổng nợ → áp phần thừa vào debt cũ nhất (cho phép âm) + raise OverRefunded event
        if (remaining > 0)
        {
            var first = orderedDebts[0];
            first.ApplyReturn(remaining, returnId);
            if (!touchedDebts.Contains(first))
                touchedDebts.Add(first);

            // Raise event để handler tạo CustomerRefund cho khoản hoàn tiền
            customerReturn.MarkOverRefunded(remaining, first.Id);
            await customerReturnRepository.UpdateAsync(customerReturn).ConfigureAwait(false);
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

    private decimal GetTotalDeliveredQuantity(Guid deliveryNoteId, Guid productId)
    {
        var deliveryNote = deliveryNoteDataReader.DataSource
            .FirstOrDefault(dn => dn.Id == deliveryNoteId && dn.Status == DeliveryNoteStatus.Delivered);

        if (deliveryNote is null) return 0;

        return deliveryNote.Items
            .Where(item => item.ProductId == productId)
            .Sum(item => item.Quantity);
    }
}
