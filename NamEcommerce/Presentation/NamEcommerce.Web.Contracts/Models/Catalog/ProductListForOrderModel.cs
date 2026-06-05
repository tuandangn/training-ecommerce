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
        public string? UnitMeasurement { get; set; }
        public int QuantityDecimalPlaces { get; set; }
        public string? PictureUrl { get; set; }
        public string? UnitMeasurementName { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal QuantityAvailable { get; set; }

        public string? CategoryName { get; set; }

        public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];
        public IEnumerable<ProductWarehouseStockModel> AvailableWarehouseStocks { get; set; } = [];
        public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableVendors { get; set; } = [];
    }

    [Serializable]
    public sealed record ProductWarehouseStockModel
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public required decimal QuantityOnHand { get; init; }
        public required decimal QuantityReserved { get; init; }
        public required decimal QuantityAvailable { get; init; }
    }
}
