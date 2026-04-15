using NamEcommerce.Web.Contracts.Models.Common;
using System.Collections;

namespace NamEcommerce.Web.Models.Preparations;

[Serializable]
public sealed class ProductPreparationListModel : IEnumerable<ProductPreparationListModel.PreparationItemModel>, IEnumerable
{
    public required IPagedDataModel<PreparationItemModel> Items { get; set; }

    public IEnumerator<PreparationItemModel> GetEnumerator()
        => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Serializable]
    public sealed record PreparationItemModel
    {
        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }

        public required decimal TotalQuantity { get; init; }
        public DateTime? EarliestShippingDate { get; init; }

        public required IList<PreparationCustomerDetailModel> CustomerDetails { get; init; }
    }

    [Serializable]
    public sealed record PreparationCustomerDetailModel
    {
        public required Guid OrderItemId { get; init; }
        public required Guid OrderId { get; init; }
        public required string OrderCode { get; init; }

        public required Guid CustomerId { get; init; }
        public required string CustomerName { get; init; }
        public string? CustomerPhone { get; init; }

        public required decimal Quantity { get; init; }
        public DateTime? ExpectedShippingDate { get; init; }

        public bool IsDelivered { get; init; }
    }

}
