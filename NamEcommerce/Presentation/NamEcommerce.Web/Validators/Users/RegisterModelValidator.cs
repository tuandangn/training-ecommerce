using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Users;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Users;

public sealed class RegisterModelValidator : AbstractValidator<RegisterModel>
{
    public RegisterModelValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.Username)
            .NotEmpty().WithMessage(m => localizer["Register.Username.Required"])
            .MinimumLength(6).WithMessage(m => localizer["Register.Username.MinLength"])
            .MaximumLength(100).WithMessage(m => localizer["Register.Username.MaxLength"]);

        RuleFor(m => m.Password)
            .NotEmpty().WithMessage(m => localizer["Register.Password.Required"])
            .MinimumLength(6).WithMessage(m => localizer["Register.Password.MinLength"]);

        RuleFor(m => m.ConfirmPassword)
            .NotEmpty().WithMessage(m => localizer["Register.ConfirmPassword.Required"])
            .Equal(m => m.Password).WithMessage(m => localizer["Register.ConfirmPassword.NotMatch"]);

        RuleFor(m => m.Fullname)
            .NotEmpty().WithMessage(m => localizer["Register.Fullname.Required"])
            .MaximumLength(100).WithMessage(m => localizer["Register.Fullname.MaxLength"]);

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage(m => localizer["Register.Phone.Required"])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Register.Phone.Invalid"]);

        RuleFor(m => m.Address)
            .MaximumLength(300).WithMessage(m => localizer["Register.Address.MaxLength"]);
    }
}
