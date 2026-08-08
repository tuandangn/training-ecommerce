using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class EditPurchaseOrderItemValidator : AbstractValidator<EditPurchaseOrderItemModel>
{
    public EditPurchaseOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(m => m.PurchaseOrderItemId)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Product"]]);

        RuleFor(m => m.UnitCost)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitCost"]]);

        RuleFor(m => m.Quantity)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(p => p.Note)
            .MaximumLength(500).WithMessage(p => localizer["Error.MaxLength", localizer["Label.Note"], 500]);
    }
}
