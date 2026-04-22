using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class AddPurchaseOrderItemValidator : AbstractValidator<AddPurchaseOrderItemModel>
{
    public AddPurchaseOrderItemValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["PurchaseOrder.Id.NotFound"]);

        RuleFor(m => m.ProductId)
            .NotEmpty().WithMessage(m => localizer["PurchaseOrder.ProductId.Required"]);

        RuleFor(m => m.UnitCost)
            .GreaterThan(0).WithMessage(m => localizer["PurchaseOrder.UnitCost.Invalid"]);

        RuleFor(m => m.Quantity)
            .GreaterThan(0).WithMessage(m => localizer["PurchaseOrder.Quantity.Invalid"]);
    }
}
