using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed class ProductListForOrderModel
{
    public string? Keywords { get; set; }
    public string? FilteredByVendorName { get; set; }

    public required IPagedDataModel<ProductItemModel> Data { get; init; }

    [Serializable]
    public sealed record ProductItemModel(Guid Id)
    {
        public required string Name { get; init; }
        public string? PictureUrl { get; set; }
        public string? UnitMeasurementName { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal QuantityAvailable { get; set; }

        public IList<Guid> AvailableWarehouseIds { get; set; } = [];
        public IList<VendorOptionModel> AvailableVendors { get; set; } = [];
    }

    [Serializable]
    public sealed record VendorOptionModel(Guid Id, string Name);
}