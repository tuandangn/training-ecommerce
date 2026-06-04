using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed class CreateUnitMeasurementModel
{
    [Display(Name = "Tên đơn vị tính")]
    public string? Name { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Cho phép số thập phân")]
    public int DecimalPlaces { get; set; }
}
