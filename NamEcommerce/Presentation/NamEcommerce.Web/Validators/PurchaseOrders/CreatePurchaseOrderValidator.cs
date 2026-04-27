using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Contracts.Extensions;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderModel>
{
    public CreatePurchaseOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.VendorId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.VendorId"]]);

        RuleFor(p => p.PlacedOn)
            .LessThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.PlacedOrderDateCannotBeInFuture"]);

        RuleFor(p => p.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Now.Date).WithMessage(p => localizer["Error.ExpectedDeliveryDateCannotBeInPast"])
            .Must((m, expectedDeliveryDate) => !expectedDeliveryDate.HasValue || expectedDeliveryDate.ToEndOfDate() >= m.PlacedOn)
            .WithMessage(p => localizer["Error.ExpectedDeliveryDateLessThanPlaceOrderDate"]);
    }
}
