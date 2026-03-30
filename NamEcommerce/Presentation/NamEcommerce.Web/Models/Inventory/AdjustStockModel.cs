using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Inventory;

public sealed record AdjustStockModel
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    public Guid WarehouseId { get; init; }

    [Display(Name = "Số lượng tồn mới")]
    [Required(ErrorMessage = "Bắt buộc nhập số lượng")]
    [Range(0, 1000000, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
    public decimal NewQuantity { get; init; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
    public string? Note { get; init; }
}
