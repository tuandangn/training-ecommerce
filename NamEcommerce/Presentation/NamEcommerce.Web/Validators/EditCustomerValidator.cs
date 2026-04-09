using FluentValidation;
using NamEcommerce.Web.Models.Customers;

namespace NamEcommerce.Web.Validators;

public sealed class EditCustomerValidator : AbstractValidator<EditCustomerModel>
{
    public EditCustomerValidator()
    {
        RuleFor(m => m.FullName)
            .NotEmpty().WithMessage("Vui lòng nhập tên khách hàng.")
            .MaximumLength(200).WithMessage("Tên khách hàng độ dài không quá 200 ký tự.");

        RuleFor(m => m.Address)
            .NotEmpty().WithMessage("Vui lòng nhập địa chỉ.")
            .MaximumLength(500).WithMessage("Địa chỉ độ dài không quá 500 ký tự.");

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage("Vui lòng nhập số điện thoại.")
            .Matches(@"0\d{9,10}").WithMessage("Số điện thoại không đúng.");
    }
}
