using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Catalog;

public sealed class EditUnitMeasurementValidator : AbstractValidator<EditUnitMeasurementModel>
{
    public EditUnitMeasurementValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["UnitMeasurement.Name.Required"])
            .MaximumLength(200).WithMessage(m => localizer["UnitMeasurement.Name.MaxLength"]);
    }
}
