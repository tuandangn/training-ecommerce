using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed record ProductForOrderModel(Guid Id)
{
    public required string Name { get; init; }
    public string? PictureUrl { get; set; }

    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityAvailable { get; set; }

    public int VendorCount { get; set; }
    public Guid? FirstVendorId { get; set; }

    public string? UnitMeasurement { get; set; }
    public string? CategoryName { get; set; }

    public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableWarehouses { get; set; } = [];
    public IEnumerable<EntityOptionListModel.EntityOptionModel> AvailableVendors { get; set; } = [];
}
