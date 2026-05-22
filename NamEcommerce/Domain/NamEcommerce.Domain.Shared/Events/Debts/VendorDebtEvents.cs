namespace NamEcommerce.Domain.Shared.Events.Debts;

/// <summary>
/// Phiếu công nợ NCC vừa được tạo (từ PurchaseOrder hoặc GoodsReceipt).
/// </summary>
public sealed record VendorDebtCreated(
    Guid VendorDebtId,
    Guid VendorId,
    decimal TotalAmount,
    Guid? PurchaseOrderId,
    Guid? GoodsReceiptId) : DomainEvent;

/// <summary>
/// Phiếu công nợ NCC được cập nhật (apply payment hoặc đổi due date / paid).
/// </summary>
public sealed record VendorDebtUpdated(Guid VendorDebtId) : DomainEvent;

/// <summary>
/// Phiếu công nợ NCC đã được trả hết (fully paid).
/// </summary>
public sealed record VendorDebtFullyPaid(Guid VendorDebtId, Guid VendorId) : DomainEvent;

/// <summary>
/// Công nợ NCC bị âm — số tiền hoàn từ phiếu trả vượt quá tổng công nợ còn lại; phần dư áp lên
/// debt đầu tiên khiến <c>RemainingAmount &lt; 0</c>. Audit để kế toán xử lý (vendor đang nợ mình).
/// </summary>
public sealed record VendorDebtBecameNegative(
    Guid VendorDebtId,
    Guid VendorId,
    Guid VendorReturnId,
    decimal OverAmount,
    decimal NewRemainingAmount) : DomainEvent;
