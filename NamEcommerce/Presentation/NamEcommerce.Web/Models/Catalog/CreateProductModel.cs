using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed class CreateProductModel
{
    [Display(Name = "Tên hàng hóa")]
    public string? Name { get; set; }

    [Display(Name = "Mô tả ngắn")]
    public string? ShortDesc { get; set; }

    [Display(Name = "Danh mục")]
    public Guid? CategoryId { get; set; }
    [ValidateNever]
    public required EntityOptionListModel AvailableCategories { get; set; }

    [Display(Name = "Đơn vị tính")]
    public Guid? UnitMeasurementId { get; set; }
    [ValidateNever]
    public required EntityOptionListModel AvailableUnitMeasurements { get; set; }

    [Display(Name = "Quản lý kho hàng")]
    public bool TrackInventory { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hình ảnh")]
    public Base64ImageModel? ImageFile { get; set; } = new();
}
