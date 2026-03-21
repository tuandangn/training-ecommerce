using NamEcommerce.Admin.Contracts.Models.Common;

namespace NamEcommerce.Admin.Contracts.Models.Catalog;

[Serializable]
public sealed class UnitMeasurementListModel
{
    public IEnumerable<UnitMeasurementModel> UnitMeasurements { get; set; }
        = Enumerable.Empty<UnitMeasurementModel>();
    public PageInfoModel? PagerInfo { get; set; }
}
