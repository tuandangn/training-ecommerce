using FluentValidation;
using NamEcommerce.Web.Models.PurchaseOrders;

namespace NamEcommerce.Web.Validators.PurchaseOrders;

public sealed class QuickCreatePurchaseOrderValidator : AbstractValidator<QuickCreatePurchaseOrderModel>
{
    public QuickCreatePurchaseOrderValidator()
    {
    }
}
