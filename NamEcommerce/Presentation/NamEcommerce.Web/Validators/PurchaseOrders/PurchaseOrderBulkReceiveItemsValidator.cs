using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class PurchaseOrderBulkReceiveItemsValidator : AbstractValidator<PurchaseOrderBulkReceiveItemsModel>
{
    public PurchaseOrderBulkReceiveItemsValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.PurchaseOrderId)
            .NotEmpty().WithMessage(m => localizer["Error.Invalid", localizer["Label.Code"]]);

        RuleFor(p => p.ReceivedOn)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.ReceivedOn"]])
            .LessThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.ReceivedDateCannotBeInFuture"]);

        RuleFor(m => m.AdditionalShipping)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.ShippingAmountCannotBeNegative"]);

        RuleFor(m => m.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage(m => localizer["Error.ExpenseTaxRateInvalid"]);

        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Items"]]);
        RuleForEach(p => p.Items).SetValidator(new BulkReceiveLineValidator(localizer));
    }
}

public sealed class BulkReceiveLineValidator : AbstractValidator<PurchaseOrderBulkReceiveItemsModel.BulkReceiveLineModel>
{
    public BulkReceiveLineValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.ItemId)
            .NotEmpty().WithMessage(m => localizer["Error.PurchaseOrderItemRequired"]);

        RuleFor(m => m.Quantity)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(m => m.ActualUnitCost)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitCost"]]);

        When(HasDirectShipRequest, () =>
        {
            RuleFor(m => m.DirectShipOrderId)
                .NotEmpty().WithMessage(m => localizer["Error.OrderRequired"]);

            RuleFor(m => m.DirectShipOrderItemId)
                .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Product"]]);

            RuleFor(m => m.DirectShipContactPhone)
                .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Phone"]]);

            RuleFor(m => m.DirectShipAddress)
                .NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Address"]]);
        }).Otherwise(() =>
        {
            RuleFor(m => m.WarehouseId)
                .NotEmpty().WithMessage(m => localizer["Error.Warehouse"]);
        });
    }

    private static bool HasDirectShipRequest(PurchaseOrderBulkReceiveItemsModel.BulkReceiveLineModel model)
        => model.DirectShipOrderId.HasValue || model.DirectShipOrderItemId.HasValue;
}
