using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Returns;

[Serializable]
public sealed class CustomerReturnListModel
{
    public Guid? CustomerId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public int? Status { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required string Code { get; init; }
        public required string CustomerName { get; init; }
        public required Guid DeliveryNoteId { get; init; }
        public required string DeliveryNoteCode { get; init; }
        public required string WarehouseName { get; init; }
        public required int Status { get; init; }
        public required DateTime ReturnDate { get; init; }
        public required decimal TotalAmount { get; init; }
        public required int ItemCount { get; init; }
    }
}
