using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class EditPurchaseOrderValidator : AbstractValidator<EditPurchaseOrderModel>
{
    public EditPurchaseOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.VendorId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Vendor"]]);

        RuleFor(p => p.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.Invalid", localizer["Label.ExpectedDate"]]);

        RuleFor(p => p.ShippingAmount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.ShippingAmount"]]);

        RuleFor(p => p.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["Error.Invalid", localizer["Label.TaxAmount"]]);
    }
}
