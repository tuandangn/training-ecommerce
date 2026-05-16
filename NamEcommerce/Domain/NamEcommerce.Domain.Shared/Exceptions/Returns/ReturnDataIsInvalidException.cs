namespace NamEcommerce.Domain.Shared.Exceptions.Returns;

/// <summary>
/// Thrown khi dữ liệu phiếu trả hàng không hợp lệ (items rỗng, quantity <= 0...).
/// <c>errorCode</c> truyền vào để dùng cho localization.
/// </summary>
[Serializable]
public sealed class ReturnDataIsInvalidException(string errorCode, params object[] parameters)
    : NamEcommerceDomainException(errorCode, parameters);
