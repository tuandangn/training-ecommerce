using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditVendorValidator : AbstractValidator<EditVendorModel>
{
    public EditVendorValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Vendor.Name.Required"])
            .MaximumLength(200).WithMessage(m => localizer["Vendor.Name.MaxLength"]);

        RuleFor(m => m.PhoneNumber)
            .NotEmpty().WithMessage(m => localizer["Vendor.Phone.Required"])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Vendor.Phone.Invalid"]);

        RuleFor(m => m.Address)
            .MaximumLength(400).WithMessage(m => localizer["Vendor.Address.MaxLength"]);
    }
}
