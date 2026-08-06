using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Finance;

[Serializable]
public sealed class ExpenseListModel
{
    public string? Keywords { get; init; }
    public string? FromDate { get; init; }
    public string? ToDate { get; init; }
    public int? ExpenseType { get; init; }
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; } = true;

    public decimal TotalAmount { get; set; }
    public required IPagedDataModel<ExpenseItemModel> Data { get; init; }

    [Serializable]
    public sealed class ExpenseItemModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Amount { get; init; }
        public int ExpenseType { get; init; }
        public DateTime IncurredDate { get; init; }
        public Guid? SourceOrderId { get; init; }
        public Guid? SourceCustomerReturnId { get; init; }
        public Guid? SourceVendorReturnId { get; init; }
        public bool IsSystemGenerated { get; init; }
    }
}
