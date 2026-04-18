using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditProductValidator : AbstractValidator<EditProductModel>
{
    public EditProductValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên hàng hóa")
            .MaximumLength(200).WithMessage("Độ dài tên hàng hóa phải nhỏ hơn 200 ký tự");

        RuleFor(m => m.ShortDesc)
            .MaximumLength(800).WithMessage("Độ dài mô tả ngắn phải nhỏ hơn 800 ký tự");

        RuleFor(m => m.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá vốn phải lớn hơn hoặc bằng 0");

        RuleFor(m => m.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Giá bán phải lớn hơn hoặc bằng 0");
    }
}
