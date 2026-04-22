using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class AddOrderItemValidator : AbstractValidator<AddOrderItemModel>
{
    public AddOrderItemValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.OrderId)
            .NotEmpty().WithMessage(m => localizer["Order.Id.NotFound"]);

        RuleFor(m => m.ProductId)
            .NotEmpty().WithMessage(m => localizer["Order.ProductId.NotFound"]);

        RuleFor(m => m.Quantity)
            .NotEmpty().WithMessage(m => localizer["Order.Quantity.Required"])
            .GreaterThan(0).WithMessage(m => localizer["Order.Quantity.Invalid"]);

        RuleFor(m => m.UnitPrice)
            .NotEmpty().WithMessage(m => localizer["Order.UnitPrice.Required"])
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Order.UnitPrice.Invalid"]);
    }
}
