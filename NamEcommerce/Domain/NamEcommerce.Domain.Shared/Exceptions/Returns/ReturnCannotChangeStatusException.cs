namespace NamEcommerce.Domain.Shared.Exceptions.Returns;

/// <summary>
/// Thrown khi cố chuyển trạng thái phiếu trả hàng theo luồng không hợp lệ
/// (ví dụ: Confirmed → Draft, Cancel phiếu Confirmed...).
/// </summary>
[Serializable]
public sealed class ReturnCannotChangeStatusException(string currentStatus, string targetStatus)
    : NamEcommerceDomainException("Error.Return.CannotChangeStatus", currentStatus, targetStatus);
