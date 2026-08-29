using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class PurchaseOrderSingleReceiveItemsValidator : AbstractValidator<PurchaseOrderSingleReceiveItemsModel>
{
    public PurchaseOrderSingleReceiveItemsValidator(IStringLocalizer<SharedResource> localizer)
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

        RuleFor(m => m.PurchaseOrderItemId)
            .NotEmpty().WithMessage(m => localizer["Error.PurchaseOrderItemRequired"]);

        RuleFor(m => m.Quantity)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(m => m.ActualUnitCost)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitCost"]]);

        RuleFor(m => m.SellingPrice)
            .GreaterThan(0).WithMessage(m => localizer["Error.Invalid", localizer["Label.UnitPrice"]]);

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
    private static bool HasDirectShipRequest(PurchaseOrderSingleReceiveItemsModel model)
        => model.DirectShipOrderId.HasValue || model.DirectShipOrderItemId.HasValue;
}
