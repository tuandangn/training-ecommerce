using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Orders;

public sealed class OrderListModel
{
    public string? Keywords { get; init; }
    public int? Status { get; set; }

    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; set; }
        public required Guid CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public required decimal TotalAmount { get; init; }
        public int Status { get; init; }
        public int PaymentStatus { get; init; }
        public int ShippingStatus { get; init; }
        public DateTime ExpectedShippingDate { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool CanUpdateInfo { get; set; }
    }
}
