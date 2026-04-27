using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Contracts.Extensions;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class EditPurchaseOrderValidator : AbstractValidator<EditPurchaseOrderModel>
{
    public EditPurchaseOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.VendorId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.VendorId"]]);

        RuleFor(p => p.PlacedOn)
            .LessThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.PlacedOrderDateCannotBeInFuture"]);

        RuleFor(p => p.ExpectedDeliveryDate)
            .Must((m, expectedDeliveryDate) => !expectedDeliveryDate.HasValue || expectedDeliveryDate.ToEndOfDate() >= m.PlacedOn)
            .WithMessage(p => localizer["Error.ExpectedDeliveryDateLessThanPlaceOrderDate"]);

        RuleFor(p => p.ShippingAmount)
            .GreaterThanOrEqualTo(0).When(p => p.ShippingAmount.HasValue)
            .WithMessage(p => localizer["Error.Invalid", localizer["Label.ShippingAmount"]]);

        RuleFor(p => p.TaxAmount)
            .GreaterThanOrEqualTo(0).When(p => p.TaxAmount.HasValue)
            .WithMessage(p => localizer["Error.Invalid", localizer["Label.TaxAmount"]]);
    }
}
