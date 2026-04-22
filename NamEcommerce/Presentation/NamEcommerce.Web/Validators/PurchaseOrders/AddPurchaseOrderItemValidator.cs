using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class AddPurchaseOrderItemValidator : AbstractValidator<AddPurchaseOrderItemModel>
{
    public AddPurchaseOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.ProductId)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Product"]]);

        RuleFor(m => m.UnitCost)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitCost"]]);

        RuleFor(m => m.Quantity)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);
    }
}
