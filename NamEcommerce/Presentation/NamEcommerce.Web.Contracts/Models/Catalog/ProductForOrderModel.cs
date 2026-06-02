using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record ProductForOrderModel(Guid Id)
{
    public required string Name { get; init; }
    public string? PictureUrl { get; set; }
    public string? UnitMeasurement { get; set; }
    public int QuantityDecimalPlaces { get; init; } = 0;
    public string? CategoryName { get; set; }
    public int VendorCount => AvailableVendors.Count();
    public Guid? FirstVendorId => AvailableVendors.FirstOrDefault()?.Id;
    public decimal CurrentUnitPrice { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }

    public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];
    public IList<EntityOptionListModel.EntityOptionModel> AvailableVendors { get; set; } = [];
}
