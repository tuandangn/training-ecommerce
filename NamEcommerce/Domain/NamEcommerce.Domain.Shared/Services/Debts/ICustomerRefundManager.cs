using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICustomerRefundManager
{
    Task<CustomerRefundDto> CreateAsync(CreateCustomerRefundDto dto);

    Task<CustomerRefundDto> CompleteAsync(Guid id, PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId);

    Task<CustomerRefundDto> CancelAsync(Guid id);

    Task<CustomerRefundDto?> GetByIdAsync(Guid id);

    Task<IPagedDataDto<CustomerRefundDto>> GetListAsync(int pageIndex, int pageSize,
        Guid? customerId = null, int? status = null, string? keywords = null);
}
