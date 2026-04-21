using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface IVendorDebtManager
{
    /// <summary>
    /// Tạo công nợ từ đơn nhập hàng (idempotent — trả về existing nếu đã tồn tại).
    /// </summary>
    Task<VendorDebtDto> CreateDebtFromPurchaseOrderAsync(CreateVendorDebtDto dto);

    /// <summary>
    /// Ghi nhận thanh toán cho 1 phiếu nợ cụ thể.
    /// </summary>
    Task<VendorPaymentDto> RecordPaymentAsync(CreateVendorPaymentDto dto);

    /// <summary>
    /// Thanh toán linh động: phân bổ tiền vào các debt còn lại của NCC theo FIFO.
    /// Nếu tiền thừa, lưu làm AdvancePayment.
    /// </summary>
    Task<IList<VendorPaymentDto>> RecordFlexiblePaymentForVendorAsync(CreateVendorPaymentDto dto);

    /// <summary>
    /// Ghi nhận tiền ứng trước cho NCC (chưa gắn phiếu nợ cụ thể).
    /// </summary>
    Task<VendorPaymentDto> RecordAdvancePaymentAsync(CreateVendorPaymentDto dto);

    Task<VendorDebtDto?> GetDebtByIdAsync(Guid id);

    Task<VendorPaymentDto?> GetPaymentByIdAsync(Guid paymentId);

    Task<VendorDebtSummaryDto?> GetVendorDebtSummaryAsync(Guid vendorId);

    /// <summary>Danh sách NCC có công nợ, gom nhóm và tính tổng. Dùng cho trang List.</summary>
    Task<IPagedDataDto<VendorDebtSummaryDto>> GetVendorsWithDebtsAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    /// <summary>Toàn bộ công nợ của 1 NCC kèm tiền ứng trước và lịch sử thanh toán. Dùng cho trang Details.</summary>
    Task<VendorDebtsByVendorDto?> GetDebtsByVendorIdAsync(Guid vendorId);

    Task<IPagedDataDto<VendorDebtDto>> GetDebtsAsync(
        Guid? vendorId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<IPagedDataDto<VendorPaymentDto>> GetPaymentsAsync(
        Guid? vendorId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
