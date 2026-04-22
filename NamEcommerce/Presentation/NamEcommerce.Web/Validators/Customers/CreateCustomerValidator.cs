using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Customers;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Customers;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerModel>
{
    public CreateCustomerValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.FullName)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.FullName"]])
            .MaximumLength(200).WithMessage(m => localizer["Error.MaxLength", localizer["Label.FullName"], 200]);

        RuleFor(m => m.Address)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Address"]])
            .MaximumLength(500).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Address"], 500]);

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Phone"]])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Error.PhoneNumberInvalid"]);
    }
}
