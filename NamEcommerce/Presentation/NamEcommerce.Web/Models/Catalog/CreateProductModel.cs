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

    [Display(Name = "Giá bán")]
    public decimal? UnitPrice { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hình ảnh")]
    public Base64ImageModel? ImageFile { get; set; }

    // Tồn kho ban đầu
    [ValidateNever]
    public ProductInventoryModel? ProductInventory { get; set; }

    public bool HasExistingStockQuantity =>
        ProductInventory?.ProductStocks.Any(s => s.Quantity > 0) ?? false;

    public sealed class ProductInventoryModel
    {
        public List<ProductStockModel> ProductStocks { get; set; } = [];
    }

    public sealed class ProductStockModel
    {
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}
