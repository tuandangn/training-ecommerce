using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Models.Debts;

public sealed class CustomerRefundListSearchModel
{
    public Guid? FilterCustomerId { get; set; }
    public int? FilterStatus { get; set; }
    public string? FilterKeywords { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public CustomerRefundListModel? Data { get; set; }
}

public sealed class CustomerRefundDetailsViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid CustomerReturnId { get; set; }
    public string CustomerReturnCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CustomerRefundStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public DateTime? RefundedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public CompleteRefundModel CompleteForm { get; set; } = new();
}

public sealed class CompleteRefundModel
{
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Note { get; set; }
}
