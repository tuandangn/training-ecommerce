using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Users;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Users;

public sealed class RegisterModelValidator : AbstractValidator<RegisterModel>
{
    public RegisterModelValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.Username)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Username"]])
            .MinimumLength(6).WithMessage(m => localizer["Error.MinLength", localizer["Label.Username"], 6])
            .MaximumLength(100).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Username"], 100]);

        RuleFor(m => m.Password)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Password"]])
            .MinimumLength(6).WithMessage(m => localizer["Error.MinLength", localizer["Label.Password"], 6]);

        RuleFor(m => m.ConfirmPassword)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.ConfirmPassword"]])
            .Equal(m => m.Password).WithMessage(m => localizer["Error.NotMatch", localizer["Label.ConfirmPassword"]]);

        RuleFor(m => m.Fullname)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.FullName"]])
            .MaximumLength(100).WithMessage(m => localizer["Error.MaxLength", localizer["Label.FullName"], 100]);

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Phone"]])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Error.PhoneNumberInvalid"]);

        RuleFor(m => m.Address)
            .MaximumLength(300).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Address"], 300]);
    }
}
