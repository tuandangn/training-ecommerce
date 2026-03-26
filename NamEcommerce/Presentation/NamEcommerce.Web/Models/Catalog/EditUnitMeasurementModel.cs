using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record EditUnitMeasurementModel
{
    public Guid Id { get; set; }

    [Display(Name = "Tên đơn vị tính")]
    public required string Name { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }
}
