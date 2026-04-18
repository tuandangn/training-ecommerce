using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record EditProductModel
{
    public Guid Id { get; set; }

    [Display(Name = "Tên hàng hóa")]
    public required string Name { get; set; }

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

    [Display(Name = "Giá nhập")]
    [UIHint("Currency")]
    public decimal CostPrice { get; set; }

    [Display(Name = "Giá bán")]
    [UIHint("Currency")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Lý do")]
    public string? ChangePriceReason { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hình ảnh")]
    public Base64ImageModel? ImageFile { get; set; } = new();
}
