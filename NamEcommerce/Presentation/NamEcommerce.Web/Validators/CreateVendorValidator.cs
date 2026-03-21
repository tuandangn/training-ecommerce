using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators;

public sealed class CreateVendorValidator : AbstractValidator<CreateVendorModel>
{
    public CreateVendorValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên nhà cung cấp")
            .MaximumLength(200).WithMessage("Độ dài tên nhà cung cấp phải nhỏ hơn 200 ký tự");
    }
}
