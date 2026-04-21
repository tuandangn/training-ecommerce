using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

public sealed class RecordCustomerPaymentCommand : IRequest<CommonActionResultModel>
{
    public required RecordPaymentModel Model { get; init; }
}

/// <summary>
/// Thanh toán linh động: không gắn vào debt cụ thể, hệ thống tự phân bổ FIFO.
/// </summary>
public sealed class RecordFlexiblePaymentCommand : IRequest<CommonActionResultModel>
{
    public required RecordPaymentModel Model { get; init; }
}
