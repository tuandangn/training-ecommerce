using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditProductValidator : AbstractValidator<EditProductModel>
{
    public EditProductValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Product.Name.Required"])
            .MaximumLength(200).WithMessage(m => localizer["Product.Name.MaxLength"]);

        RuleFor(m => m.ShortDesc)
            .MaximumLength(800).WithMessage(m => localizer["Product.ShortDesc.MaxLength"]);

        RuleFor(m => m.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Product.CostPrice.Invalid"]);

        RuleFor(m => m.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Product.UnitPrice.Invalid"]);
    }
}
