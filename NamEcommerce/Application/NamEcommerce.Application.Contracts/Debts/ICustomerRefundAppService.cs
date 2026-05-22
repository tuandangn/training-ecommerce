using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICustomerRefundAppService
{
    Task<CustomerRefundAppDto?> GetByIdAsync(Guid id);

    Task<IPagedDataAppDto<CustomerRefundAppDto>> GetListAsync(
        Guid? customerId = null,
        int? status = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<CompleteCustomerRefundResultAppDto> CompleteAsync(CompleteCustomerRefundAppDto dto);

    Task<CancelCustomerRefundResultAppDto> CancelAsync(Guid id);
}
