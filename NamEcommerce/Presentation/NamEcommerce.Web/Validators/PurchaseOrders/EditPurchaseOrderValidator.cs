using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class EditPurchaseOrderValidator : AbstractValidator<EditPurchaseOrderModel>
{
    public EditPurchaseOrderValidator(IStringLocalizer<ValidationResource> localizer)
    {
        RuleFor(p => p.VendorId)
            .NotEmpty().WithMessage(p => localizer["PurchaseOrder.VendorId.Required"]);

        RuleFor(p => p.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["PurchaseOrder.ExpectedDate.Invalid"]);

        RuleFor(p => p.ShippingAmount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["PurchaseOrder.ShippingAmount.Invalid"]);

        RuleFor(p => p.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage(p => localizer["PurchaseOrder.TaxAmount.Invalid"]);
    }
}
