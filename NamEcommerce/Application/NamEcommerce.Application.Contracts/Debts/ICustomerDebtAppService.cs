using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICustomerDebtAppService
{
    Task<CustomerPaymentAppDto> RecordPaymentAsync(CreateCustomerPaymentAppDto dto);
    
    Task<CustomerDebtAppDto?> GetDebtByIdAsync(Guid id);
    
    Task<PagedDataAppDto<CustomerDebtAppDto>> GetDebtsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<PagedDataAppDto<CustomerPaymentAppDto>> GetPaymentsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
