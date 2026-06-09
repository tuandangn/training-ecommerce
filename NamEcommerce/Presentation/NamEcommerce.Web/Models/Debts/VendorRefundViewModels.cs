using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Models.Debts;

public sealed class VendorRefundListSearchModel
{
    public Guid? FilterVendorId { get; set; }
    public int? FilterStatus { get; set; }
    public string? FilterKeywords { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public VendorRefundListModel? Data { get; set; }
}

public sealed class VendorRefundDetailsViewModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid VendorReturnId { get; set; }
    public string VendorReturnCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public VendorRefundStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public PaymentMethod? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public DateTime? RefundedOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public CompleteVendorRefundModel CompleteForm { get; set; } = new();
}

public sealed class CompleteVendorRefundModel
{
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Note { get; set; }
}
