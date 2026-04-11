using FluentValidation;
using NamEcommerce.Web.Models.Users;

namespace NamEcommerce.Web.Validators.Users;

public sealed class RegisterModelValidator : AbstractValidator<RegisterModel>
{
    public RegisterModelValidator()
    {
        RuleFor(m => m.Username)
            .NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập")
            .MinimumLength(6).WithMessage("Tên đăng nhập phải có ít nhất 6 ký tự")
            .MaximumLength(100).WithMessage("Tên đăng nhập không được vượt quá 100 ký tự");
        RuleFor(m => m.Password)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu")
            .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự");
        RuleFor(m => m.ConfirmPassword)
            .NotEmpty().WithMessage("Vui lòng nhập xác nhận mật khẩu")
            .Equal(m => m.Password).WithMessage("Mật khẩu không trùng khớp");
        RuleFor(m => m.Fullname)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên")
            .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự");
        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage("Vui lòng nhập số điện thoại")
            .Matches(@"0\d{9,10}").WithMessage("Số điện thoại không đúng");
        RuleFor(m => m.Address)
            .MaximumLength(300).WithMessage("Địa chỉ không được vượt quá 200 ký tự");
    }
}
