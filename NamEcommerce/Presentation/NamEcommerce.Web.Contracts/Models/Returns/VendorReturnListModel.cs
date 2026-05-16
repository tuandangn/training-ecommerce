using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Returns;

[Serializable]
public sealed class VendorReturnListModel
{
    public Guid? VendorId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? GoodsReceiptId { get; init; }
    public int? Status { get; init; }
    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required string Code { get; init; }
        public required string VendorName { get; init; }
        public required string WarehouseName { get; init; }
        public required int Status { get; init; }
        public required DateTime ReturnDate { get; init; }
        public required decimal TotalAmount { get; init; }
        public required int ItemCount { get; init; }
    }
}
