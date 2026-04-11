using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class CreateProductValidator : AbstractValidator<CreateProductModel>
{
    public CreateProductValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên hàng hóa")
            .MaximumLength(200).WithMessage("Độ dài tên hàng hóa phải nhỏ hơn 200 ký tự");

        RuleFor(m => m.ShortDesc)
            .MaximumLength(800).WithMessage("Độ dài mô tả ngắn phải nhỏ hơn 800 ký tự");
    }
}
