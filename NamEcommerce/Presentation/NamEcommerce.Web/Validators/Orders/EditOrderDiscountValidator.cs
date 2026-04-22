using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class EditOrderDiscountValidator : AbstractValidator<EditOrderDiscountModel>
{
    public EditOrderDiscountValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Order.Id.NotFound"]);

        RuleFor(m => m.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Order.Discount.Invalid"]);
    }
}
