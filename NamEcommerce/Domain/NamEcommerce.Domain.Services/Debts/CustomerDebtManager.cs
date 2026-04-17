using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
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
        
        if (customer == null) throw new ArgumentException("Customer not found");
        if (deliveryNote == null) throw new ArgumentException("Delivery note not found");

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
        );

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

        var inserted = await debtRepository.InsertAsync(debt).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto)
    {
        dto.Verify();

        var customer = await customerReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer == null) throw new ArgumentException("Customer not found");

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
                await debtRepository.UpdateAsync(debt).ConfigureAwait(false);
            }
        }

        var inserted = await paymentRepository.InsertAsync(payment).ConfigureAwait(false);
        return MapToPaymentDto(inserted);
    }

    public async Task<CustomerDebtDto?> GetDebtByIdAsync(Guid id)
    {
        var debt = await debtReader.GetByIdAsync(id).ConfigureAwait(false);
        if (debt == null) return null;
        
        var dto = MapToDto(debt);
        // Note: In a real app we might want to include payments list here
        return dto;
    }

    public async Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(Guid? customerId = null, int pageIndex = 0, int pageSize = 15)
    {
        var query = debtReader.DataSource;
        if (customerId.HasValue)
            query = query.Where(d => d.CustomerId == customerId.Value);
            
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
