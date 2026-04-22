using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditProductValidator : AbstractValidator<EditProductModel>
{
    public EditProductValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Name"]])
            .MaximumLength(200).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Name"], 200]);

        RuleFor(m => m.ShortDesc)
            .MaximumLength(800).WithMessage(m => localizer["Error.MaxLength", localizer["Label.ShortDesc"], 800]);

        RuleFor(m => m.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.CostPrice"]]);

        RuleFor(m => m.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitPrice"]]);
    }
}
