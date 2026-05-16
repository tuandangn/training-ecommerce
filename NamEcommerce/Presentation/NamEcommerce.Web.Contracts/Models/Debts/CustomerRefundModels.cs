namespace NamEcommerce.Web.Contracts.Models.Debts;

[Serializable]
public sealed class CustomerRefundModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid CustomerReturnId { get; set; }
    public string CustomerReturnCode { get; set; } = string.Empty;
    public Guid? CustomerDebtId { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public int? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public DateTime? RefundedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
}

[Serializable]
public sealed class CustomerRefundListModel
{
    public IList<CustomerRefundModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public Guid? FilterCustomerId { get; set; }
    public int? FilterStatus { get; set; }
    public string? FilterKeywords { get; set; }
}
