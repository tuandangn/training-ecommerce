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

    [Display(Name = "Công nợ ban đầu")]
    public decimal? InitialDebt { get; set; }
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

    public bool HasPortalAccount { get; set; }
    public int? PortalAccountStatus { get; set; }
    public bool HasPortalPassword { get; set; }
    public DateTime? PortalPasswordSetOnUtc { get; set; }
    public DateTime? PortalLastLoginOnUtc { get; set; }
    public DateTime? PortalUpdatedOnUtc { get; set; }
}
