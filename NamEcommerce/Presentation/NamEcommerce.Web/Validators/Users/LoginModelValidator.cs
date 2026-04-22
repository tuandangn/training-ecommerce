using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Users;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Users;

public sealed class LoginModelValidator : AbstractValidator<LoginModel>
{
    public LoginModelValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.Username)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Username"]]);
        RuleFor(m => m.Password)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Password"]]);
    }
}
