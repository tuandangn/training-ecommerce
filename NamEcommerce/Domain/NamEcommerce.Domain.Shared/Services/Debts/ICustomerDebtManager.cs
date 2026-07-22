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
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<CustomerDebtsByCustomerDto?> GetDebtsByCustomerIdAsync(Guid customerId);

    Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(
        Guid? customerId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<IPagedDataDto<CustomerPaymentDto>> GetPaymentsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<decimal> GetTotalPaidByOrderAsync(Guid orderId);

    Task<decimal> GetTotalDebtByOrderAsync(Guid orderId);

    Task<decimal> GetTotalPaidByDeliveryNoteAsync(Guid deliveryNoteId);
}
