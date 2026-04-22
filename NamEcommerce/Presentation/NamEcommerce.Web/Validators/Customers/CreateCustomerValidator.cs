using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Customers;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Customers;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerModel>
{
    public CreateCustomerValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.FullName)
            .NotEmpty().WithMessage(m => localizer["Customer.FullName.Required"])
            .MaximumLength(200).WithMessage(m => localizer["Customer.FullName.MaxLength"]);

        RuleFor(m => m.Address)
            .NotEmpty().WithMessage(m => localizer["Customer.Address.Required"])
            .MaximumLength(500).WithMessage(m => localizer["Customer.Address.MaxLength"]);

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage(m => localizer["Customer.Phone.Required"])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Customer.Phone.Invalid"]);
    }
}
