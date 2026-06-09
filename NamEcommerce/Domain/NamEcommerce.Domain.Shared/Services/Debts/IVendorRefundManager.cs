using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface IVendorRefundManager
{
    /// <summary>
    /// Tạo phiếu thu tiền hoàn từ NCC — gọi từ <c>VendorReturnOverRecoveredEventHandler</c>.
    /// </summary>
    Task<VendorRefundDto> CreateAsync(CreateVendorRefundDto dto);

    /// <summary>
    /// Xác nhận đã thu tiền từ NCC (Pending → Completed).
    /// </summary>
    Task<VendorRefundDto> CompleteAsync(Guid id, PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId);

    /// <summary>
    /// Huỷ phiếu thu tiền hoàn (Pending → Cancelled).
    /// </summary>
    Task<VendorRefundDto> CancelAsync(Guid id);

    Task<VendorRefundDto?> GetByIdAsync(Guid id);

    Task<IPagedDataDto<VendorRefundDto>> GetListAsync(
        Guid? vendorId = null,
        int? status = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);
}
