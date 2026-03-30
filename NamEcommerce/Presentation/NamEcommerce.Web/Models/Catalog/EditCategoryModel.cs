using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Web.Contracts.Models.Common;
using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Web.Models.Catalog;

[Serializable]
public sealed record EditCategoryModel
{
    public Guid Id { get; set; }

    [Display(Name = "Tên danh mục")]
    public string? Name { get; set; }

    [Display(Name = "Danh mục cha")]
    public Guid? ParentId { get; set; }
    [ValidateNever]
    public required EntityOptionListModel AvailableParents { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }
}
