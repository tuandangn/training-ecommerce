using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Inventory;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseModel>
{
    public CreateWarehouseValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.Code)
            .NotEmpty().WithMessage(m => localizer["Warehouse.Code.Required"])
            .MaximumLength(50).WithMessage(m => localizer["Warehouse.Code.MaxLength"]);

        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Warehouse.Name.Required"])
            .MaximumLength(200).WithMessage(m => localizer["Warehouse.Name.MaxLength"]);

        RuleFor(m => m.PhoneNumber)
            .Matches(@"\s*|(0\d{9,10})").WithMessage(m => localizer["Warehouse.Phone.Invalid"]);

        RuleFor(m => m.Address)
            .MaximumLength(800).WithMessage(m => localizer["Warehouse.Address.MaxLength"]);
    }
}
