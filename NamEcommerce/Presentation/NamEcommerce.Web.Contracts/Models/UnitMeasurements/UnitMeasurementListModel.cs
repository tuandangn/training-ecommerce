using NamEcommerce.Web.Contracts.Common;

namespace NamEcommerce.Web.Contracts.Models.UnitMeasurements;

[Serializable]
public sealed class UnitMeasurementListModel
{
    public string? Keywords { get; set; }

    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id, string Name)
    {
        public int DisplayOrder { get; init; }
    }
}
