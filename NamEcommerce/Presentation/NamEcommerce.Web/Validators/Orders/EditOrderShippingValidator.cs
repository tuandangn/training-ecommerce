using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderShippingValidator : AbstractValidator<EditOrderShippingModel>
{
    public EditOrderShippingValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(p => p.ExpectedShippingDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage(p => localizer["Error.Invalid", localizer["Label.ExpectedDate"]]);

        RuleFor(m => m.Address)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Address"]])
            .MaximumLength(1000).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Address"], 1000]);

        RuleFor(m => m.Note)
            .MaximumLength(1000).WithMessage(m => localizer["Error.MaxLength", localizer["Label.Note"], 1000]);
    }
}
