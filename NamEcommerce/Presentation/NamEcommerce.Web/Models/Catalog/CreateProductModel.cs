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
    public EntityOptionListModel? AvailableCategories { get; set; }

    [Display(Name = "Đơn vị tính")]
    public Guid? UnitMeasurementId { get; set; }
    [ValidateNever]
    public EntityOptionListModel? AvailableUnitMeasurements { get; set; }

    [Display(Name = "Nhà cung cấp")]
    public IList<Guid> VendorIds { get; set; } = [];
    [ValidateNever]
    public EntityOptionListModel? AvailableVendors { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hình ảnh")]
    public Base64ImageModel? ImageFile { get; set; }

    [Display(Name = "Đã có hàng tồn")]
    public bool HasExistingStockQuantity { get; set; }
    public ProductInventoryModel? ProductInventory { get; set; }

    [Serializable]
    public sealed class ProductInventoryModel
    {
        [Display(Name = "Giá vốn")]
        [UIHint("Currency")]
        public decimal CostPrice { get; set; }

        [Display(Name = "Giá bán")]
        [UIHint("Currency")]
        public decimal UnitPrice { get; set; }

        public IList<ProductStockModel> ProductStocks { get; set; } = [];
    }

    [Serializable]
    public sealed class ProductStockModel
    {
        [Display(Name = "Kho hàng")]
        public Guid? WarehouseId { get; set; }
        [ValidateNever]
        public string? WarehouseName { get; set; }

        [Display(Name = "Số lượng")]
        public decimal Quantity { get; set; }
    }
}
