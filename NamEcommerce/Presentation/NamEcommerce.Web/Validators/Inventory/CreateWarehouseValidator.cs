using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Inventory;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators;

public sealed class CreateWarehouseValidator : AbstractValidator<CreateWarehouseModel>
{
    public CreateWarehouseValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.Code)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Code"]])
            .MaximumLength(50).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Code"], 50]);

        RuleFor(m => m.Name)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Name"]])
            .MaximumLength(200).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Name"], 200]);

        RuleFor(m => m.PhoneNumber)
            .Matches(@"\s*|(0\d{9,10})").WithMessage(m => localizer["Error.PhoneNumberInvalid"]);

        RuleFor(m => m.Address)
            .MaximumLength(800).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Address"], 800]);
    }
}
