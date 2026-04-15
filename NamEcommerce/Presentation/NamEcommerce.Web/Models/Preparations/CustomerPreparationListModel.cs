using NamEcommerce.Web.Contracts.Models.Common;
using System.Collections;

namespace NamEcommerce.Web.Models.Preparations;

[Serializable]
public sealed class CustomerPreparationListModel : IEnumerable<CustomerPreparationListModel.PreparationItemModel>, IEnumerable
{
    public required IPagedDataModel<PreparationItemModel> Items { get; set; }

    public IEnumerator<PreparationItemModel> GetEnumerator()
        => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Serializable]
    public sealed record PreparationItemModel
    {
        public required Guid OrderItemId { get; init; }
        public required Guid OrderId { get; init; }
        public required string OrderCode { get; init; }

        public required Guid ProductId { get; init; }
        public required string ProductName { get; init; }

        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }

        public required Guid CustomerId { get; init; }
        public required string CustomerName { get; init; }
        public string? CustomerPhone { get; init; }

        public DateTime? ExpectedShippingDate { get; init; }

        public bool IsDelivered { get; init; }
    }

}
