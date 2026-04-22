using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class ReceivePurchaseOrderItemValidator : AbstractValidator<ReceivePurchaseOrderItemModel>
{
    public ReceivePurchaseOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.PurchaseOrderItemId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Product"]]);

        RuleFor(m => m.ReceivedQuantity)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(m => m.SellingPrice!.Value)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitPrice"]])
            .When(m => m.SellingPrice.HasValue);
    }
}
