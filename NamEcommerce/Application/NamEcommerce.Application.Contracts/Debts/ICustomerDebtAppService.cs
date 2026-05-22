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

    Task<PagedDataAppDto<CustomerDebtSummaryAppDto>> GetCustomersWithDebtsAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<CustomerDebtsByCustomerAppDto?> GetDebtsByCustomerIdAsync(Guid customerId);

    Task<PagedDataAppDto<CustomerDebtAppDto>> GetDebtsAsync(
        Guid? customerId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<PagedDataAppDto<CustomerPaymentAppDto>> GetPaymentsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
