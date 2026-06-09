namespace NamEcommerce.Web.Contracts.Models.Debts;

[Serializable]
public sealed class VendorRefundModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid VendorReturnId { get; set; }
    public string VendorReturnCode { get; set; } = string.Empty;
    public Guid? VendorDebtId { get; set; }
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public int? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public DateTime? RefundedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
}

[Serializable]
public sealed class VendorRefundListModel
{
    public IList<VendorRefundModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public Guid? FilterVendorId { get; set; }
    public int? FilterStatus { get; set; }
    public string? FilterKeywords { get; set; }
}
