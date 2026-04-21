using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

/// <summary>Ghi nhận thanh toán cho 1 phiếu nợ cụ thể của NCC.</summary>
public sealed class RecordVendorPaymentCommand : IRequest<CommonActionResultModel>
{
    public required RecordVendorPaymentModel Model { get; init; }
}

/// <summary>
/// Thanh toán linh động: không gắn vào debt cụ thể, hệ thống tự phân bổ FIFO.
/// Tiền thừa được lưu làm ứng trước (AdvancePayment).
/// </summary>
public sealed class RecordFlexibleVendorPaymentCommand : IRequest<CommonActionResultModel>
{
    public required RecordVendorPaymentModel Model { get; init; }
}

/// <summary>Ghi nhận tiền ứng trước cho NCC (chưa gắn phiếu nợ cụ thể).</summary>
public sealed class RecordVendorAdvancePaymentCommand : IRequest<CommonActionResultModel>
{
    public required RecordVendorPaymentModel Model { get; init; }
}
