using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Customers;

public sealed class CustomerListSearchModel
{
    public string? Keywords { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

public sealed class CreateCustomerModel
{
    [Display(Name = "Họ tên")]
    public string? FullName { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public sealed class EditCustomerModel
{
    public Guid Id { get; set; }

    [Display(Name = "Họ tên")]
    public string? FullName { get; set; }

    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}
