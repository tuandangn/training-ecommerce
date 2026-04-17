using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICustomerDebtManager
{
    Task<CustomerDebtDto> CreateDebtFromDeliveryNoteAsync(CreateCustomerDebtDto dto);
    
    Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto);
    
    Task<CustomerDebtDto?> GetDebtByIdAsync(Guid id);
    
    Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<IPagedDataDto<CustomerPaymentDto>> GetPaymentsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
