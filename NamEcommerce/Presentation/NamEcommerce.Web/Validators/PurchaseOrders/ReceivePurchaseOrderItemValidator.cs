using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class ReceivePurchaseOrderItemValidator : AbstractValidator<ReceivePurchaseOrderItemModel>
{
    public ReceivePurchaseOrderItemValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["PurchaseOrder.Id.NotFound"]);

        RuleFor(m => m.PurchaseOrderItemId)
            .NotEmpty().WithMessage(m => localizer["PurchaseOrder.ItemId.NotFound"]);

        RuleFor(m => m.ReceivedQuantity)
            .GreaterThan(0).WithMessage(m => localizer["PurchaseOrder.Quantity.Invalid"]);

        RuleFor(m => m.SellingPrice!.Value)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["PurchaseOrder.SellingPrice.Invalid"])
            .When(m => m.SellingPrice.HasValue);
    }
}
