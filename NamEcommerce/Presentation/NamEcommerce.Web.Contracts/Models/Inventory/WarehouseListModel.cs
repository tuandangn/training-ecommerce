using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed class WarehouseListModel
{
    public string? Keywords { get; set; }
    public required IPagedDataModel<WarehouseItemModel> Data { get; init; }

    [Serializable]
    public sealed record WarehouseItemModel(Guid Id)
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? WarehouseType { get; init; }
        public bool IsActive { get; init; }
    }
}

