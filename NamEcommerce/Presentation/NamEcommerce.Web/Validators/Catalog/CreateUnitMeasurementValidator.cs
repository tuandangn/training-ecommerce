using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class CreateUnitMeasurementValidator : AbstractValidator<CreateUnitMeasurementModel>
{
    public CreateUnitMeasurementValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Name"]])
            .MaximumLength(200).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Name"], 200]);
    }
}
