using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderModel>
{
    public CreatePurchaseOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.VendorId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Vendor"]]);

        RuleFor(p => p.PlacedOn)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.PlaceOrderDate"]])
            .LessThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.PlacedOrderDateCannotBeInFuture"]);

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Items"]]);
        RuleForEach(p => p.Items).SetValidator(p => new CreatePurchaseOrderItemValidator(localizer));
    }
}
public sealed class CreatePurchaseOrderItemValidator : AbstractValidator<CreatePurchaseOrderItemModel>
{
    public CreatePurchaseOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.ProductId)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Product"]]);

        RuleFor(m => m.UnitCost)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.UnitCost"]])
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitCost"]]);
        RuleFor(m => m.Quantity)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Quantity"]])
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);
    }
}
