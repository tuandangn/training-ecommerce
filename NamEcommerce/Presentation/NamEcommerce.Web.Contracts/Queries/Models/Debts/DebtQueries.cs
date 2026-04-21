using MediatR;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Queries.Models.Debts;

/// <summary>Lấy danh sách khách hàng có công nợ (gom nhóm theo khách hàng).</summary>
public sealed class GetCustomerDebtListQuery : IRequest<CustomerDebtListModel>
{
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

/// <summary>Lấy toàn bộ công nợ + tiền cọc + lịch sử của 1 khách hàng.</summary>
public sealed class GetCustomerDebtDetailsQuery : IRequest<CustomerDebtDetailsModel?>
{
    public required Guid CustomerId { get; init; }
}

public sealed class GetCustomerPaymentReceiptQuery : IRequest<CustomerPaymentReceiptModel?>
{
    public required Guid PaymentId { get; init; }
}
