using NamEcommerce.Admin.Client.Models.Common;

namespace NamEcommerce.Admin.Client.Models.Catalog;

[Serializable]
public sealed class UnitMeasurementListModel
{
    public IEnumerable<UnitMeasurementModel> UnitMeasurements { get; set; }
        = Enumerable.Empty<UnitMeasurementModel>();
    public PageInfoModel? PagerInfo { get; set; }
}
