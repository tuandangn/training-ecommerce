using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICustomerRefundManager
{
    /// <summary>
    /// Tạo phiếu hoàn tiền — gọi từ <c>CustomerReturnOverRefundedEventHandler</c>.
    /// </summary>
    Task<CustomerRefundDto> CreateAsync(CreateCustomerRefundDto dto);

    /// <summary>
    /// Xác nhận đã hoàn tiền cho khách (Pending → Completed).
    /// </summary>
    Task<CustomerRefundDto> CompleteAsync(Guid id, PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId);

    /// <summary>
    /// Huỷ phiếu hoàn tiền (Pending → Cancelled).
    /// </summary>
    Task<CustomerRefundDto> CancelAsync(Guid id);

    Task<CustomerRefundDto?> GetByIdAsync(Guid id);

    Task<IPagedDataDto<CustomerRefundDto>> GetListAsync(
        Guid? customerId = null,
        int? status = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);
}
