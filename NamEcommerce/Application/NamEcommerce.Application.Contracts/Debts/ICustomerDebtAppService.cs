using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICustomerDebtAppService
{
    Task<CreateInitialCustomerDebtResultAppDto> CreateInitialDebtAsync(CreateInitialCustomerDebtAppDto dto);

    Task<CustomerPaymentAppDto> RecordPaymentAsync(CreateCustomerPaymentAppDto dto);

    Task<IList<CustomerPaymentAppDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentAppDto dto);

    Task<CustomerDebtAppDto?> GetDebtByIdAsync(Guid id);

    Task<CustomerPaymentAppDto?> GetPaymentByIdAsync(Guid paymentId);

    Task<CustomerDebtSummaryAppDto?> GetCustomerDebtSummaryAsync(Guid customerId);

    Task<IPagedDataAppDto<CustomerDebtSummaryAppDto>> GetCustomersWithDebtsAsync(
        int pageIndex = 0,
        int pageSize = 15,
        string? keywords = null);

    Task<CustomerDebtsByCustomerAppDto?> GetDebtsByCustomerIdAsync(Guid customerId);

    Task<IPagedDataAppDto<CustomerDebtAppDto>> GetDebtsAsync(
        int pageIndex = 0,
        int pageSize = 15,
        Guid? customerId = null,
        string? keywords = null);

    Task<IPagedDataAppDto<CustomerPaymentAppDto>> GetPaymentsAsync(
        int pageIndex = 0, int pageSize = 15,
        Guid? customerId = null, Guid? orderId = null);

    Task<decimal> GetTotalPaidByOrderAsync(Guid orderId);
    Task<decimal> GetTotalDebtByOrderAsync(Guid orderId);
}
