using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderModel>
{
    public CreateOrderValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(p => p.CustomerId)
            .NotEmpty().WithMessage(p => localizer["Order.CustomerId.Required"]);

        RuleFor(p => p.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Order.Discount.Invalid"]);

        RuleFor(p => p.ExpectedShippingDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage(p => localizer["Order.ExpectedDate.Invalid"]);

        RuleFor(p => p.ShippingAddress)
            .NotEmpty().WithMessage(p => localizer["Order.ShippingAddress.Required"])
            .MaximumLength(1000).WithMessage(p => localizer["Order.ShippingAddress.MaxLength"]);

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Order.Items.Required"]);
    }
}

public sealed class CreateOrderItemValidator : AbstractValidator<CreateOrderItemModel>
{
    public CreateOrderItemValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(p => p.ProductId)
            .NotEmpty().WithMessage(p => localizer["Order.ProductId.Required"]);

        RuleFor(p => p.Quantity)
            .NotEmpty().WithMessage(p => localizer["Order.Quantity.Required"])
            .GreaterThan(0).WithMessage(p => localizer["Order.Quantity.Invalid"]);

        RuleFor(p => p.UnitPrice)
            .NotEmpty().WithMessage(p => localizer["Order.UnitPrice.Required"])
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Order.UnitPrice.Invalid"]);
    }
}
