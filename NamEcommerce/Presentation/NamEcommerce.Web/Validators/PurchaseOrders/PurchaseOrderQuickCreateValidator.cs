using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class PurchaseOrderQuickCreateValidator : AbstractValidator<PurchaseOrderQuickCreateModel>
{
    public PurchaseOrderQuickCreateValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(m => m.VendorId).NotEmpty().WithMessage(m => localizer["Error.Required", localizer["Label.Vendor"]]);

        RuleFor(p => p.PlacedOn)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.PlaceOrderDate"]])
            .LessThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.PlacedOrderDateCannotBeInFuture"]);


        RuleFor(p => p.Items)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Items"]]);
        RuleForEach(p => p.Items).SetValidator(m => new PurchaseOrderQuickCreateItemValidator(localizer));

        RuleFor(p => p.Note)
            .MaximumLength(500).WithMessage(p => localizer["Error.MaxLength", localizer["Label.Note"], 500]);

        When(p => p.IsReceived, () =>
        {
            RuleFor(p => p.DefaultWarehouseId)
                .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Warehouse"]]);

            RuleFor(p => p.ReceivedOn)
                .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.ReceivedOn"]])
                .GreaterThanOrEqualTo(p => p.PlacedOn).WithMessage(p => localizer["Error.ReceivedOnMustBeAfterPlacedOn"])
                .LessThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.ReceivedDateCannotBeInFuture"]);

            RuleFor(p => p.ShippingAmount)
                .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.ShippingAmount"]]);
        }).Otherwise(() =>
        {
            RuleFor(p => p.ExpectedDeliveryDate)
                .GreaterThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.ExpectedDeliveryDateCannotBeInPast"]);
        });

        When(p => p.IsPaid, () =>
        {
            RuleFor(p => p.PaidAmount)
                .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.PaidAmount"]])
                .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.PaidAmount"]])
                .LessThanOrEqualTo(m => m.Items.Sum(item => item.SubTotal)).WithMessage(p => localizer["Error.PaidAmountExceedsOrderTotal"]);
        });
        When(p => p.IsPaid && p.PaymentMethod == PaymentMethod.BankTransfer, () =>
        {
            RuleFor(p => p.BankAccountId)
                .NotEmpty().WithMessage(p => localizer["Error.BankTransferMethodRequireBankAccount"]);
        });
    }
}

public sealed class PurchaseOrderQuickCreateItemValidator : AbstractValidator<PurchaseOrderQuickCreateModel.QuickCreatePurchaseOrderItemModel>
{
    public PurchaseOrderQuickCreateItemValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.ProductId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Product"]]);

        RuleFor(p => p.Quantity)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Quantity"]])
            .GreaterThan(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.Quantity"]]);

        RuleFor(p => p.UnitCost)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.UnitCost"]])
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.UnitCost"]]);
    }
}
