using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditVendorValidator : AbstractValidator<EditVendorModel>
{
    public EditVendorValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Name"]])
            .MaximumLength(200).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Name"], 200]);

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Phone"]])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Error.PhoneNumberInvalid"]);

        RuleFor(m => m.Address)
            .MaximumLength(400).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Address"], 400]);
    }
}
