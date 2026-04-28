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
