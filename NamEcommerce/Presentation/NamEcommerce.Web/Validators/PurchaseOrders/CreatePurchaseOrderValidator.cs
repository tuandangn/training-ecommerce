using FluentValidation;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderModel>
{
    public CreatePurchaseOrderValidator(IStringLocalizer<SharedResource> localizer)
    {
        RuleFor(p => p.VendorId)
            .NotEmpty().WithMessage(p => localizer["Error.Required", localizer["Label.Vendor"]]);

        RuleFor(p => p.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Now).WithMessage(p => localizer["Error.Invalid", localizer["Label.ExpectedDate"]]);
    }
}
