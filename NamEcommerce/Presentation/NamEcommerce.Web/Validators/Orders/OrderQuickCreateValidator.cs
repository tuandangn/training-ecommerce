using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.Orders;

public sealed class OrderQuickCreateValidator : AbstractValidator<OrderQuickCreateModel>
{
    public OrderQuickCreateValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.CustomerId).NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Customer"]]);

        RuleFor(p => p.OrderDiscount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.Discount"]]);

        RuleFor(p => p.ShippingAddress)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Address"]])
            .MaximumLength(500).WithMessage(p => localizer["Error.MaxLength", localizer["Label.Address"], 500]);

        RuleFor(p => p.ShippingPhoneNumber)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Phone"]])
            .MaximumLength(50).WithMessage(p => localizer["Error.MaxLength", localizer["Label.Phone"], 50])
            .Matches(@"0\d{9,10}").WithMessage(m => localizer["Error.PhoneNumberInvalid"]);

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Items"]]);
        RuleForEach(p => p.Items).SetValidator(m => new OrderQuickCreateItemValidator(localizer));

        RuleFor(p => p.Note)
            .MaximumLength(500).WithMessage(p => localizer["Error.MaxLength", localizer["Label.Note"], 500]);
    }
}

public sealed class OrderQuickCreateItemValidator : AbstractValidator<QuickCreateOrderItemModel>
{
    public OrderQuickCreateItemValidator(IStringLocalizer<SharedResource> localizer)
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
