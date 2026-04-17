using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Debts;

public sealed class CustomerDebtSearchModel
{
    public Guid? CustomerId { get; set; }
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public sealed class CustomerDebtListModel
{
    public string? Keywords { get; set; }
    public IPagedDataModel<CustomerDebtListItemModel>? Data { get; set; }
}

public sealed record CustomerDebtListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string CustomerName { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public required string OrderCode { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    
    public int Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime? DueDateUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

public sealed record CustomerDebtDetailsModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public required string OrderCode { get; init; }
    public required Guid OrderId { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    
    public int Status { get; init; }
    public string StatusName { get; init; } = string.Empty;
    public DateTime? DueDateUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    
    public IList<CustomerPaymentListItemModel> Payments { get; init; } = [];
}

public sealed record CustomerPaymentListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public string PaymentMethodName { get; init; } = string.Empty;
    public int PaymentType { get; init; }
    public string PaymentTypeName { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime PaidOnUtc { get; init; }
    public string? OrderCode { get; init; }
}

public sealed class RecordPaymentModel
{
    public Guid CustomerId { get; set; }
    public Guid? CustomerDebtId { get; set; }
    public Guid? OrderId { get; set; }
    
    public decimal Amount { get; set; }
    public int PaymentMethod { get; set; }
    public int PaymentType { get; set; }
    public string? Note { get; set; }
    public DateTime PaidOnUtc { get; set; } = DateTime.UtcNow;
}
