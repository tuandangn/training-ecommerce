using MediatR;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Queries.Models.Debts;

public sealed class GetCustomerDebtListQuery : IRequest<CustomerDebtListModel>
{
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public sealed class GetCustomerDebtDetailsQuery : IRequest<CustomerDebtDetailsModel?>
{
    public required Guid CustomerId { get; init; }
}

public sealed class GetCustomerPaymentReceiptQuery : IRequest<CustomerPaymentReceiptModel?>
{
    public required Guid PaymentId { get; init; }
}

public sealed class GetCustomerLedgerListQuery : IRequest<CustomerLedgerListModel>
{
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public sealed class GetCustomerLedgerDetailsQuery : IRequest<CustomerLedgerDetailsModel?>
{
    public required Guid CustomerId { get; init; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
