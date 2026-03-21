using FluentValidation;
using NamEcommerce.Web.Models.Users;

namespace NamEcommerce.Web.Validators;

public sealed class LoginModelValidator : AbstractValidator<LoginModel>
{
    public LoginModelValidator()
    {
        RuleFor(m => m.Username)
            .NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập");
        RuleFor(m => m.Password)
            .NotEmpty().WithMessage("Vui lòng nhập mật khẩu");
    }
}
