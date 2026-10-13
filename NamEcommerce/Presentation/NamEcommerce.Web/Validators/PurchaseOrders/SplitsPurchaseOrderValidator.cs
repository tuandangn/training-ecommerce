using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class SplitsPurchaseOrderValidator : AbstractValidator<SplitsPurchaseOrderModel>
{
    public SplitsPurchaseOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Items"]]);
        RuleForEach(p => p.Items).SetValidator(p => new SplitsPurchaseOrderItemValidator(localizer));
    }
}

public sealed class SplitsPurchaseOrderItemValidator : AbstractValidator<SplitsPurchaseOrderModel.SplitsPurchaseOrderItemModel>
{
    public SplitsPurchaseOrderItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.PurchaseOrderItemId"]]);

        RuleFor(m => m.Quantity)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Quantity"]])
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);
    }
}
