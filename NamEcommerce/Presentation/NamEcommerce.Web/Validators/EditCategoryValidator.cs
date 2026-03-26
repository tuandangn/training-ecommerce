using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators;

public sealed class EditCategoryValidator : AbstractValidator<EditCategoryModel>
{
    public EditCategoryValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên danh mục")
            .MaximumLength(200).WithMessage("Độ dài tên danh mục phải nhỏ hơn 200 ký tự");
    }
}
