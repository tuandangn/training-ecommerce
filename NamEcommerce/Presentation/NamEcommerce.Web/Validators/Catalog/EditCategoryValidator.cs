using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditCategoryValidator : AbstractValidator<EditCategoryModel>
{
    public EditCategoryValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Category.Name.Required"])
            .MaximumLength(200).WithMessage(m => localizer["Category.Name.MaxLength"]);
    }
}
