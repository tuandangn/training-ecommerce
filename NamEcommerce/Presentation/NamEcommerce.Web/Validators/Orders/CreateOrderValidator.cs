using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderModel>
{
    public CreateOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.CustomerId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Customer"]]);

        RuleFor(p => p.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.Discount"]]);

        RuleFor(p => p.ExpectedShippingDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage(p => localizer["Error.Invalid", localizer["Label.ExpectedDate"]]);

        RuleFor(p => p.ShippingAddress)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Address"]])
            .MaximumLength(1000).WithMessage(p => localizer["Error.MaxLength", localizer["Label.Address"], 1000]);

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Items"]]);
    }
}

public sealed class CreateOrderItemValidator : AbstractValidator<CreateOrderItemModel>
{
    public CreateOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.ProductId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Product"]]);

        RuleFor(p => p.Quantity)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Quantity"]])
            .GreaterThan(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(p => p.UnitPrice)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.UnitPrice"]])
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.UnitPrice"]]);
    }
}
