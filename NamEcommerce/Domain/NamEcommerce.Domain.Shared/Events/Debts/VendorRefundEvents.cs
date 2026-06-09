namespace NamEcommerce.Domain.Shared.Events.Debts;

/// <summary>
/// Phiếu thu tiền hoàn từ NCC vừa được tạo (do trả hàng NCC vượt mức nợ còn lại).
/// </summary>
public sealed record VendorRefundCreated(
    Guid VendorRefundId,
    Guid VendorId,
    decimal Amount) : DomainEvent;

/// <summary>
/// Phiếu thu tiền hoàn từ NCC đã được thực hiện — tiền đã thu về từ NCC.
/// </summary>
public sealed record VendorRefundCompleted(
    Guid VendorRefundId,
    Guid VendorId,
    decimal Amount) : DomainEvent;

/// <summary>
/// Phiếu thu tiền hoàn từ NCC bị huỷ — không thực hiện thu tiền.
/// </summary>
public sealed record VendorRefundCancelled(Guid VendorRefundId) : DomainEvent;
