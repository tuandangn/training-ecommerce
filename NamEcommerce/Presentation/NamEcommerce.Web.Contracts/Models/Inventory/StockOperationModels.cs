using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Contracts.Models.Inventory;

[Serializable]
public sealed class ReserveStockModel
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    [Display(Name = "Số lượng")]
    [Required(ErrorMessage = "Bắt buộc nhập số lượng")]
    [Range(0.01, 1000000, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; }

    [Display(Name = "Mã tham chiếu")]
    public Guid? ReferenceId { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
    public string? Note { get; set; }
}

[Serializable]
public sealed class ReleaseReservedStockModel
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    [Display(Name = "Số lượng")]
    [Required(ErrorMessage = "Bắt buộc nhập số lượng")]
    [Range(0.01, 1000000, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; }

    [Display(Name = "Mã tham chiếu")]
    public Guid? ReferenceId { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
    public string? Note { get; set; }
}

[Serializable]
public sealed class DispatchStockModel
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    [Display(Name = "Số lượng")]
    [Required(ErrorMessage = "Bắt buộc nhập số lượng")]
    [Range(0.01, 1000000, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; }

    [Display(Name = "Mã tham chiếu")]
    public Guid? ReferenceId { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
    public string? Note { get; set; }
}

[Serializable]
public sealed class ReceiveStockModel
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    [Display(Name = "Số lượng")]
    [Required(ErrorMessage = "Bắt buộc nhập số lượng")]
    [Range(0.01, 1000000, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; }

    [Display(Name = "Loại tham chiếu")]
    [Range(0, 3, ErrorMessage = "Loại tham chiếu không hợp lệ")]
    public int ReferenceType { get; set; }

    [Display(Name = "Mã tham chiếu")]
    public Guid? ReferenceId { get; set; }

    [Display(Name = "Ghi chú")]
    [MaxLength(500, ErrorMessage = "Ghi chú không quá 500 ký tự")]
    public string? Note { get; set; }
}
