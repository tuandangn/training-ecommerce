using FluentValidation;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Validators;

public sealed class EditVendorValidator : AbstractValidator<EditVendorModel>
{
    public EditVendorValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Vui lòng nhập tên nhà cung cấp")
            .MaximumLength(200).WithMessage("Độ dài tên nhà cung cấp phải nhỏ hơn 200 ký tự");

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage("Vui lòng nhập số điện thoại")
            .Matches(@"0\d{9,10}").WithMessage("Số điện thoại không đúng");

        RuleFor(m => m.Address)
            .MaximumLength(400).WithMessage("Độ dài địa chỉ phải nhỏ hơn 400 ký tự");
    }
}
