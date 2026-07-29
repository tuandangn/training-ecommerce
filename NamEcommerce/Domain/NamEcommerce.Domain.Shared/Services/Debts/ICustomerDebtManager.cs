using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICustomerDebtManager
{
    Task<CustomerDebtDto> CreateDebtFromDeliveryNoteAsync(CreateCustomerDebtDto dto);

    Task<CustomerDebtDto> CreateInitialDebtAsync(CreateInitialCustomerDebtDto dto);
    
    Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto);

    Task<IList<CustomerPaymentDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentDto dto);

    Task<CustomerCreditNoteDto> ApplyCreditNoteFromCustomerReturnAsync(
        Guid customerId,
        Guid returnId,
        string returnCode,
        Guid? sourceDeliveryNoteId,
        decimal amount);

    Task<CustomerDebtDto?> GetDebtByIdAsync(Guid id);

    Task<CustomerPaymentDto?> GetPaymentByIdAsync(Guid paymentId);

    Task<CustomerDebtSummaryDto?> GetCustomerDebtSummaryAsync(Guid customerId);

    Task<IPagedDataDto<CustomerDebtSummaryDto>> GetCustomersWithDebtsAsync(
        int pageIndex = 0,
        int pageSize = 15,
        string? keywords = null);

    Task<CustomerDebtsByCustomerDto?> GetDebtsByCustomerIdAsync(Guid customerId);

    Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(
        int pageIndex = 0,
        int pageSize = 15,
        Guid? customerId = null,
        string? keywords = null);

    Task<IPagedDataDto<CustomerPaymentDto>> GetPaymentsAsync(
        int pageIndex = 0,
        int pageSize = 15,
        Guid? customerId = null, Guid? orderId = null);

    Task<decimal> GetTotalPaidByOrderAsync(Guid orderId);

    Task<decimal> GetTotalDebtByOrderAsync(Guid orderId);

    Task<decimal> GetTotalPaidByDeliveryNoteAsync(Guid deliveryNoteId);
}
