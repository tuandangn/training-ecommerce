using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderShippingValidator : AbstractValidator<EditOrderShippingModel>
{
    public EditOrderShippingValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Order.Id.NotFound"]);

        RuleFor(p => p.ExpectedShippingDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage(p => localizer["Order.ExpectedDate.Invalid"]);

        RuleFor(m => m.Address)
            .NotEmpty().WithMessage(m => localizer["Order.ShippingAddress.Required"])
            .MaximumLength(1000).WithMessage(m => localizer["Order.ShippingAddress.MaxLength"]);

        RuleFor(m => m.Note)
            .MaximumLength(1000).WithMessage(m => localizer["Order.Note.MaxLength"]);
    }
}
