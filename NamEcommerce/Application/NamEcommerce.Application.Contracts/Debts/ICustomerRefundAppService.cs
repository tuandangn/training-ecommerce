using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICustomerRefundAppService
{
    Task<CustomerRefundAppDto?> GetByIdAsync(Guid id);

    Task<IPagedDataAppDto<CustomerRefundAppDto>> GetListAsync(int pageIndex, int pageSize,
        Guid? customerId = null, int? status = null, string? keywords = null);

    Task<CompleteCustomerRefundResultAppDto> CompleteAsync(CompleteCustomerRefundAppDto dto);

    Task<CancelCustomerRefundResultAppDto> CancelAsync(Guid id);
}
