using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface IVendorDebtAppService
{
    /// <summary>
    /// Tạo công nợ từ đơn nhập hàng (idempotent — trả về existing nếu đã tồn tại).
    /// Không throw — lỗi được trả về trong <see cref="CreateVendorDebtResultAppDto"/>.
    /// </summary>
    Task<CreateVendorDebtResultAppDto> CreateDebtFromPurchaseOrderAsync(CreateVendorDebtAppDto dto);

    /// <summary>
    /// Ghi nhận thanh toán cho 1 phiếu nợ cụ thể.
    /// Không throw — lỗi được trả về trong <see cref="RecordVendorPaymentResultAppDto"/>.
    /// </summary>
    Task<RecordVendorPaymentResultAppDto> RecordPaymentAsync(CreateVendorPaymentAppDto dto);

    /// <summary>
    /// Thanh toán linh động: phân bổ tiền vào các debt còn lại của NCC theo FIFO.
    /// Nếu tiền thừa, lưu làm AdvancePayment.
    /// Không throw — lỗi được trả về trong <see cref="RecordVendorPaymentResultAppDto.Payments"/>.
    /// </summary>
    Task<RecordVendorPaymentResultAppDto> RecordFlexiblePaymentForVendorAsync(CreateVendorPaymentAppDto dto);

    /// <summary>
    /// Ghi nhận tiền ứng trước cho NCC (chưa gắn phiếu nợ cụ thể).
    /// Không throw — lỗi được trả về trong <see cref="RecordVendorPaymentResultAppDto"/>.
    /// </summary>
    Task<RecordVendorPaymentResultAppDto> RecordAdvancePaymentAsync(CreateVendorPaymentAppDto dto);

    Task<VendorDebtAppDto?> GetDebtByIdAsync(Guid id);

    /// <summary>
    /// Lấy phiếu nợ NCC sinh từ một <c>GoodsReceipt</c> cụ thể (nếu đã sinh).
    /// Dùng cho UI hiển thị badge "Đã ghi nợ" trên trang Details của phiếu nhập.
    /// Trả <c>null</c> nếu phiếu nhập đó chưa sinh nợ.
    /// </summary>
    Task<VendorDebtAppDto?> GetDebtByGoodsReceiptIdAsync(Guid goodsReceiptId);

    Task<VendorPaymentAppDto?> GetPaymentByIdAsync(Guid paymentId);

    Task<VendorDebtSummaryAppDto?> GetVendorDebtSummaryAsync(Guid vendorId);

    /// <summary>Danh sách NCC có công nợ, gom nhóm và tính tổng. Dùng cho trang List.</summary>
    Task<PagedDataAppDto<VendorDebtSummaryAppDto>> GetVendorsWithDebtsAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    /// <summary>Toàn bộ công nợ + tiền ứng trước + lịch sử thanh toán của 1 NCC. Dùng cho trang Details.</summary>
    Task<VendorDebtsByVendorAppDto?> GetDebtsByVendorIdAsync(Guid vendorId);

    Task<PagedDataAppDto<VendorDebtAppDto>> GetDebtsAsync(
        Guid? vendorId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<PagedDataAppDto<VendorPaymentAppDto>> GetPaymentsAsync(
        Guid? vendorId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
