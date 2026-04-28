using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class CustomerDebtManager(
    IRepository<CustomerDebt> debtRepository,
    IEntityDataReader<CustomerDebt> debtReader,
    IRepository<CustomerPayment> paymentRepository,
    IEntityDataReader<CustomerPayment> paymentReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader) : ICustomerDebtManager
{
    private async Task<string> GenerateDebtCodeAsync()
    {
        var datePrefix = $"CN-{DateTime.UtcNow:yyyyMMdd}";
        var count = debtReader.DataSource.Count(d => d.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
    }

    private async Task<string> GeneratePaymentCodeAsync()
    {
        var datePrefix = $"PT-{DateTime.UtcNow:yyyyMMdd}";
        var count = paymentReader.DataSource.Count(p => p.Code.StartsWith(datePrefix));
        return $"{datePrefix}-{(count + 1):D3}";
    }

    public async Task<CustomerDebtDto> CreateDebtFromDeliveryNoteAsync(CreateCustomerDebtDto dto)
    {
        dto.Verify();

        // 1. Check if debt already exists for this delivery note
        var existing = debtReader.DataSource.FirstOrDefault(d => d.DeliveryNoteId == dto.DeliveryNoteId);
        if (existing != null)
            return MapToDto(existing);

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(dto.DeliveryNoteId).ConfigureAwait(false);
        
        if (customer == null) throw new CustomerIsNotFoundException(dto.CustomerId);
        if (deliveryNote == null) throw new DeliveryNoteNotFoundException(dto.DeliveryNoteId);

        var code = await GenerateDebtCodeAsync().ConfigureAwait(false);
        
        var debt = new CustomerDebt(
            code: code,
            customerId: customer.Id,
            customerName: customer.FullName,
            deliveryNoteId: deliveryNote.Id,
            deliveryNoteCode: deliveryNote.Code,
            orderId: deliveryNote.OrderId,
            orderCode: deliveryNote.OrderCode ?? string.Empty,
            totalAmount: dto.TotalAmount,
            dueDateUtc: dto.DueDateUtc,
            createdByUserId: dto.CreatedByUserId
        )
        {
            CustomerAddress = customer.PhoneNumber,
            CustomerPhone = customer.PhoneNumber
        };

        // 2. Auto-allocate deposits (Tiền cọc)
        var deposits = paymentReader.DataSource
            .Where(p => p.OrderId == deliveryNote.OrderId && p.PaymentType == PaymentType.Deposit && !p.IsApplied)
            .ToList();

        foreach (var deposit in deposits)
        {
            debt.ApplyPayment(deposit.Amount);
            deposit.MarkAsApplied();
            await paymentRepository.UpdateAsync(deposit).ConfigureAwait(false);
        }

        debt.MarkCreated();
        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto)
    {
        dto.Verify();

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(dto.CustomerId);

        var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);
        
        var payment = new CustomerPayment(
            code: code,
            customerId: customer.Id,
            customerName: customer.FullName,
            amount: dto.Amount,
            paymentMethod: dto.PaymentMethod,
            paymentType: dto.PaymentType,
            paidOnUtc: dto.PaidOnUtc,
            recordedByUserId: dto.RecordedByUserId,
            note: dto.Note
        )
        {
            OrderId = dto.OrderId,
            DeliveryNoteId = dto.DeliveryNoteId,
            CustomerDebtId = dto.CustomerDebtId
        };

        // If it's a payment for a specific debt, apply it
        if (dto.CustomerDebtId.HasValue)
        {
            var debt = await debtRepository.GetByIdAsync(dto.CustomerDebtId.Value).ConfigureAwait(false);
            if (debt != null)
            {
                debt.ApplyPayment(payment.Amount);
                payment.MarkAsApplied();
                debt.MarkUpdated();
                await debtRepository.UpdateAsync(debt).ConfigureAwait(false);
            }
        }
        else if (dto.DeliveryNoteId.HasValue)
        {
            // Try to find the debt for this delivery note
            var debt = debtReader.DataSource.FirstOrDefault(d => d.DeliveryNoteId == dto.DeliveryNoteId.Value);
            if (debt != null)
            {
                debt.ApplyPayment(payment.Amount);
                payment.MarkAsApplied();
                payment.CustomerDebtId = debt.Id;
                debt.MarkUpdated();
                await debtRepository.UpdateAsync(debt).ConfigureAwait(false);
            }
        }

        payment.MarkCreated();
        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);
        return MapToPaymentDto(inserted);
    }

    public async Task<CustomerDebtDto?> GetDebtByIdAsync(Guid id)
    {
        var debt = await debtReader.GetByIdAsync(id).ConfigureAwait(false);
        if (debt == null) return null;

        // Load tất cả payments liên quan đến debt này
        var payments = paymentReader.DataSource
            .Where(p => p.CustomerDebtId == id)
            .OrderBy(p => p.PaidOnUtc)
            .ToList();

        var dto = MapToDto(debt);
        return dto with { Payments = payments.Select(MapToPaymentDto).ToList() };
    }

    public async Task<CustomerPaymentDto?> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await paymentReader.GetByIdAsync(paymentId).ConfigureAwait(false);
        return payment == null ? null : MapToPaymentDto(payment);
    }

    public async Task<CustomerDebtSummaryDto?> GetCustomerDebtSummaryAsync(Guid customerId)
    {
        var debts = debtReader.DataSource
            .Where(d => d.CustomerId == customerId)
            .ToList();

        if (debts.Count == 0) return null;

        // Lấy tên khách hàng từ bản ghi đầu tiên
        var customerName = debts[0].CustomerName;

        return new CustomerDebtSummaryDto
        {
            CustomerId = customerId,
            CustomerName = customerName,
            TotalDebtAmount = debts.Sum(d => d.TotalAmount),
            TotalPaidAmount = debts.Sum(d => d.PaidAmount),
            TotalRemainingAmount = debts.Sum(d => d.RemainingAmount),
            DebtCount = debts.Count
        };
    }

    /// <summary>
    /// Thanh toán linh động: phân bổ số tiền vào các debt còn lại của khách hàng theo thứ tự FIFO.
    /// Nếu tiền thừa sau khi trả hết tất cả nợ, lưu dưới dạng CustomerPayment không gắn debt (General).
    /// </summary>
    public async Task<IList<CustomerPaymentDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentDto dto)
    {
        dto.Verify();

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(dto.CustomerId);

        // Lấy tất cả debts chưa trả hết, sắp xếp cũ nhất trước (FIFO)
        var pendingDebts = debtReader.DataSource
            .Where(d => d.CustomerId == dto.CustomerId && d.RemainingAmount > 0)
            .OrderBy(d => d.CreatedOnUtc)
            .ToList();

        var results = new List<CustomerPaymentDto>();
        var remaining = dto.Amount;

        foreach (var debt in pendingDebts)
        {
            if (remaining <= 0) break;

            var applyAmount = Math.Min(remaining, debt.RemainingAmount);
            var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);

            var payment = new CustomerPayment(
                code: code,
                customerId: customer.Id,
                customerName: customer.FullName,
                amount: applyAmount,
                paymentMethod: dto.PaymentMethod,
                paymentType: PaymentType.DebtPayment,
                paidOnUtc: dto.PaidOnUtc,
                recordedByUserId: dto.RecordedByUserId,
                note: dto.Note
            )
            {
                CustomerDebtId = debt.Id,
                OrderId = debt.OrderId,
                DeliveryNoteId = debt.DeliveryNoteId
            };

            debt.ApplyPayment(applyAmount);
            payment.MarkAsApplied();

            debt.MarkUpdated();
            await debtRepository.UpdateAsync(debt).ConfigureAwait(false);
            payment.MarkCreated();
            var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);
            results.Add(MapToPaymentDto(inserted));

            remaining -= applyAmount;
        }

        // Nếu còn dư tiền sau khi trả hết nợ → lưu làm tiền cọc chung
        if (remaining > 0)
        {
            var code = await GeneratePaymentCodeAsync().ConfigureAwait(false);
            var overpayment = new CustomerPayment(
                code: code,
                customerId: customer.Id,
                customerName: customer.FullName,
                amount: remaining,
                paymentMethod: dto.PaymentMethod,
                paymentType: PaymentType.Deposit,
                paidOnUtc: dto.PaidOnUtc,
                recordedByUserId: dto.RecordedByUserId,
                note: string.IsNullOrEmpty(dto.Note) ? "Tiền dư sau khi thanh toán nợ" : dto.Note
            );
            overpayment.MarkCreated();
            var inserted = await paymentRepository.InsertAsync(overpayment).ConfigureAwait(false);
            results.Add(MapToPaymentDto(inserted));
        }

        return results;
    }

    public async Task<IPagedDataDto<CustomerDebtSummaryDto>> GetCustomersWithDebtsAsync(string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        // Load tất cả debts vào memory rồi group (phù hợp với quy mô cửa hàng)
        var allDebts = debtReader.DataSource.ToList();

        // Group theo CustomerId
        var groups = allDebts
            .GroupBy(d => d.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                CustomerName = g.First().CustomerName,
                CustomerPhone = g.First().CustomerPhone,
                CustomerAddress = g.First().CustomerAddress,
                TotalDebtAmount = g.Sum(d => d.TotalAmount),
                TotalPaidAmount = g.Sum(d => d.PaidAmount),
                TotalRemainingAmount = g.Sum(d => d.RemainingAmount),
                DebtCount = g.Count()
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keywords))
            groups = groups.Where(g => g.CustomerName.Contains(keywords, StringComparison.OrdinalIgnoreCase));

        var sorted = groups.OrderBy(g => g.CustomerName).ToList();
        var total = sorted.Count;
        var page = sorted.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        var results = new List<CustomerDebtSummaryDto>();
        foreach (var item in page)
        {
            var depositBalance = paymentReader.DataSource
                .Where(p => p.CustomerId == item.CustomerId
                         && p.PaymentType == PaymentType.Deposit
                         && !p.IsApplied)
                .Sum(p => p.Amount);

            results.Add(new CustomerDebtSummaryDto
            {
                CustomerId = item.CustomerId,
                CustomerName = item.CustomerName,
                CustomerPhone = item.CustomerPhone,
                CustomerAddress = item.CustomerAddress,
                TotalDebtAmount = item.TotalDebtAmount,
                TotalPaidAmount = item.TotalPaidAmount,
                TotalRemainingAmount = item.TotalRemainingAmount,
                DepositBalance = depositBalance,
                DebtCount = item.DebtCount
            });
        }

        return PagedDataDto.Create(results, pageIndex, pageSize, total);
    }

    public async Task<CustomerDebtsByCustomerDto?> GetDebtsByCustomerIdAsync(Guid customerId)
    {
        var debts = debtReader.DataSource
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.CreatedOnUtc)
            .ToList();

        if (debts.Count == 0) return null;

        // Load payments gắn với từng debt
        var debtDtos = new List<CustomerDebtDto>();
        foreach (var debt in debts)
        {
            var payments = paymentReader.DataSource
                .Where(p => p.CustomerDebtId == debt.Id)
                .OrderBy(p => p.PaidOnUtc)
                .ToList();
            debtDtos.Add(MapToDto(debt) with { Payments = payments.Select(MapToPaymentDto).ToList() });
        }

        // Tiền cọc chưa áp dụng
        var deposits = paymentReader.DataSource
            .Where(p => p.CustomerId == customerId && p.PaymentType == PaymentType.Deposit && !p.IsApplied)
            .OrderByDescending(p => p.PaidOnUtc)
            .ToList();

        // Lịch sử 20 giao dịch gần nhất
        var recentPayments = paymentReader.DataSource
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaidOnUtc)
            .Take(20)
            .ToList();

        var depositBalance = deposits.Sum(p => p.Amount);

        return new CustomerDebtsByCustomerDto
        {
            CustomerId = customerId,
            CustomerName = debts[0].CustomerName,
            TotalDebtAmount = debts.Sum(d => d.TotalAmount),
            TotalPaidAmount = debts.Sum(d => d.PaidAmount),
            TotalRemainingAmount = debts.Sum(d => d.RemainingAmount),
            DepositBalance = depositBalance,
            Debts = debtDtos,
            Deposits = deposits.Select(MapToPaymentDto).ToList(),
            RecentPayments = recentPayments.Select(MapToPaymentDto).ToList()
        };
    }

    public async Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(Guid? customerId = null, string? keywords = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = debtReader.DataSource;
        if (customerId.HasValue)
            query = query.Where(d => d.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(d => d.Code.Contains(keywords)
                || d.CustomerName.Contains(keywords)
                || d.OrderCode.Contains(keywords)
                || d.DeliveryNoteCode.Contains(keywords));

        query = query.OrderByDescending(d => d.CreatedOnUtc);

        var total = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();

        return PagedDataDto.Create(items.Select(MapToDto).ToList(), pageIndex, pageSize, total);
    }

    public async Task<IPagedDataDto<CustomerPaymentDto>> GetPaymentsAsync(Guid? customerId = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = paymentReader.DataSource;
        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);
            
        query = query.OrderByDescending(p => p.CreatedOnUtc);
        
        var total = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        
        return PagedDataDto.Create(items.Select(MapToPaymentDto).ToList(), pageIndex, pageSize, total);
    }

    private static CustomerDebtDto MapToDto(CustomerDebt debt)
    {
        return new CustomerDebtDto
        {
            Id = debt.Id,
            Code = debt.Code,
            CustomerId = debt.CustomerId,
            CustomerName = debt.CustomerName,
            DeliveryNoteId = debt.DeliveryNoteId,
            DeliveryNoteCode = debt.DeliveryNoteCode,
            OrderId = debt.OrderId,
            OrderCode = debt.OrderCode,
            TotalAmount = debt.TotalAmount,
            PaidAmount = debt.PaidAmount,
            RemainingAmount = debt.RemainingAmount,
            Status = debt.Status,
            DueDateUtc = debt.DueDateUtc,
            CreatedOnUtc = debt.CreatedOnUtc,
            CreatedByUserId = debt.CreatedByUserId
        };
    }

    private static CustomerPaymentDto MapToPaymentDto(CustomerPayment payment)
    {
        return new CustomerPaymentDto
        {
            Id = payment.Id,
            Code = payment.Code,
            CustomerId = payment.CustomerId,
            CustomerName = payment.CustomerName,
            OrderId = payment.OrderId,
            OrderCode = payment.OrderCode,
            DeliveryNoteId = payment.DeliveryNoteId,
            DeliveryNoteCode = payment.DeliveryNoteCode,
            CustomerDebtId = payment.CustomerDebtId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            PaymentType = payment.PaymentType,
            Note = payment.Note,
            PaidOnUtc = payment.PaidOnUtc,
            RecordedByUserId = payment.RecordedByUserId,
            CreatedOnUtc = payment.CreatedOnUtc
        };
    }
}
