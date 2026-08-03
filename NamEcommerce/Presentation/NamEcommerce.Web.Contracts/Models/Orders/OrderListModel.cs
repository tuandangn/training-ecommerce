using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Orders;

public sealed class OrderListModel
{
    public string? Keywords { get; init; }
    public int? Status { get; set; }
    public bool IsWaitingPayment { get; set; }

    public required IPagedDataModel<OrderModel> Data { get; init; }

    [Serializable]
    public sealed record OrderModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; set; }
        public required int OrderStatus { get; set; }
        public required Guid CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public required decimal TotalAmount { get; init; }
        public bool IsFinished { get; init; }
        public DateTime? ExpectedShippingDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool CanUpdateInfo { get; set; }
        public IList<OrderItemSummaryModel> Items { get; init; } = [];
        public decimal TotalOrdered => Items.Sum(item => item.QuantityOrdered);
        public decimal TotalDeliveryNoteQuantity => Items.Sum(item => item.QuantityInDeliveryNotes);
        public decimal TotalDelivered => Items.Sum(item => item.QuantityDelivered);
        public bool PaymentRequired { get; set; }
    }

    [Serializable]
    public sealed record OrderItemSummaryModel
    {
        public required Guid OrderItemId { get; init; }
        public required string ProductName { get; init; }
        public required decimal QuantityOrdered { get; init; }
        public decimal QuantityInDeliveryNotes { get; init; }
        public decimal QuantityDelivered { get; init; }
    }
}
